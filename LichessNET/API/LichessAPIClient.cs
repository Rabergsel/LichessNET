using System.Net;
using System.Net.Http.Headers;
using System.Web;
using Microsoft.Extensions.Logging;
using TokenBucket;
using Vertical.SpectreLogger;

namespace LichessNET.API;

/// <summary>
///     This class represents a client for the lichess API.
///     It handles all ratelimits and requests.
/// </summary>
/// <example>
///     This example shows how to initialize the LichessAPIClient.
///     <code>
///     var client = new LichessApiClient("YOUR_API_TOKEN");
///     Console.WriteLine($"User is part of {team.Count} teams.");
///     </code>
/// </example>
public partial class LichessApiClient

{
    private readonly ILogger _logger;

    /// <summary>
    ///     Bucket handler for ratelimits
    /// </summary>
    private readonly ApiRatelimitController _ratelimitController = new();

    /// <summary>
    ///     The token to access the Lichess API
    /// </summary>
    internal string Token = "";

    /// <summary>
    ///     Creates a lichess API client, according to settings
    /// </summary>
    /// <param name="token">The token for accessing the lichess API</param>
    public LichessApiClient(string token = "")
    {
        var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(Constants.MinimumLogLevel)
            .AddSpectreConsole());

        _logger = loggerFactory.CreateLogger("LichessAPIClient");


        this.Token = token;
        if (token != "")
            _logger.LogInformation("Connecting to Lichess API with token");
        else
            _logger.LogInformation("Connecting to Lichess API without token");

        if (!token.Contains("_"))
            _logger.LogWarning("The token provided may not be a valid lichess API token. Please check the token.");

        _logger.LogInformation("Connection to Lichess API established.");

        _ratelimitController.RegisterBucket("api/account", TokenBuckets.Construct().WithCapacity(5)
            .WithFixedIntervalRefillStrategy(3, TimeSpan.FromSeconds(15)).Build());

        _ratelimitController.RegisterBucket("api/streamer/live", TokenBuckets.Construct().WithCapacity(2)
            .WithFixedIntervalRefillStrategy(1, TimeSpan.FromSeconds(5)).Build());

        _ratelimitController.RegisterBucket("api/import",
            TokenBuckets.Construct().WithCapacity(200)
                .WithFixedIntervalRefillStrategy(200, TimeSpan.FromHours(1))
                .Build());
    }


    /// <summary>
    ///     Gets the UriBuilder objects for the lichess client.
    ///     If something changes in the future, it will be easy to change it.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <returns></returns>
    private UriBuilder GetUriBuilder(string endpoint, params Tuple<string, string>[] queryParameters)
    {
        var builder = new UriBuilder(Constants.BaseUrl + endpoint);
        builder.Port = -1;

        var query = HttpUtility.ParseQueryString(builder.Query);

        foreach (var param in queryParameters) query[param.Item1] = param.Item2;

        builder.Query = query.ToString();

        return builder;
    }

    private HttpRequestMessage GetRequestScaffold(string endpoint, params Tuple<string, string>[] queryParameters)
    {
        var request = new HttpRequestMessage();
        request.RequestUri = GetUriBuilder(endpoint, queryParameters).Uri;
        return request;
    }

    private async Task<HttpResponseMessage> SendRequest(HttpRequestMessage request, HttpMethod method = null,
        bool useToken = true)
    {
        if (method == null) method = HttpMethod.Get;
        _ratelimitController.Consume(request.RequestUri.AbsolutePath, true);
        var client = new HttpClient();
        if (useToken & Token != "")
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        }

        _logger.LogInformation("Sending request to " + request.RequestUri);
        var response = client.SendAsync(request).Result;
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Request to " + request.RequestUri + " successful.");
            _logger.LogDebug("Response: \n" + response.Content.ReadAsStringAsync().Result);
            return response;
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _logger.LogError("Ratelimited by Lichess API. Waiting for 60 seconds.");
            _ratelimitController.ReportBlock();
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            if ((await response.Content.ReadAsStringAsync()).Contains("Missing scope"))
            {
                _logger.LogError(
                    "The token provided does not have the required scope to access this endpoint. The client will " +
                    "resend a request without a token.");
                return await SendRequest(new HttpRequestMessage()
                {
                    RequestUri = request.RequestUri
                }, method, false);
            }
        }

        _logger.LogError("Error while fetching data from Lichess API. Status code: " + response.StatusCode);
        _logger.LogInformation("Response: \n" + response.Content.ReadAsStringAsync().Result);
        return null;
    }
}