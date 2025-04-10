using System.Net;
using System.Net.Http.Headers;
using System.Web;
using LichessNET.Entities.OAuth;
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
///     var client = new LichessApiClient();
///     client.SetToken("yourToken");
///     </code>
/// </example>
public partial class LichessApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    /// <summary>
    ///     Bucket handler for ratelimits
    /// </summary>
    private readonly ApiRatelimitController _ratelimitController = new();

    /// <summary>
    ///     The token to access the Lichess API
    /// </summary>
    private string? _token;

    /// <summary>
    ///     Creates a lichess API client, according to settings
    /// </summary>
    /// <param name="token">The token for accessing the lichess API</param>
    public LichessApiClient()
    {
        var loggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(Constants.MinimumLogLevel)
            .AddSpectreConsole());

        _logger = loggerFactory.CreateLogger("LichessAPIClient");


        _httpClient = new HttpClient();

        _ratelimitController.RegisterBucket("api/account", TokenBuckets.Construct().WithCapacity(5)
            .WithFixedIntervalRefillStrategy(3, TimeSpan.FromSeconds(15)).Build());

        _ratelimitController.RegisterBucket("api/streamer/live", TokenBuckets.Construct().WithCapacity(2)
            .WithFixedIntervalRefillStrategy(1, TimeSpan.FromSeconds(5)).Build());
    }

    /// <summary>
    /// Returns used token for the API
    /// </summary>
    /// <returns>Used token</returns>
    public string? GetToken() => _token;

    /// <summary>
    /// Sets token to use for requests to the API
    /// </summary>
    /// <param name="value">API Token. If null, this client wont use a token, resulting in a reduced ratelimit</param>
    /// <exception cref="UnauthorizedAccessException">Thrown when token is invalid</exception>
    public async Task SetToken(string? value)
    {
        if (value == null)
        {
            _token = null;
            return;
        }

        var tokenTest = await TestTokensAsync(new List<string> { value });
        if (tokenTest[value] is not null)
        {
            _token = value;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", this.GetToken());
        }
        else
        {
            throw new UnauthorizedAccessException("Invalid token");
        }
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
        bool useToken = true, HttpContent content = null)
    {
        if (method == null) method = HttpMethod.Get;
        await _ratelimitController.Consume(request.RequestUri.AbsolutePath, true);
        var client = _httpClient;

        request.Method = method;
        request.Content = content;

        _logger.LogInformation("Sending request to " + request.RequestUri);
        var response = await client.SendAsync(request);
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
            throw new HttpRequestException("Access denied. Your token does not have the required scope.");
        }

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException("Api Key is invalid.");
        }

        _logger.LogError("Error while fetching data from Lichess API. Status code: " + response.StatusCode);
        _logger.LogInformation("Response: \n" + response.Content.ReadAsStringAsync().Result);
        return null;
    }
}