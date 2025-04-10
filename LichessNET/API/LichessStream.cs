using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Vertical.SpectreLogger;

namespace LichessNET.API;

/// <summary>
/// Represents a stream sent by Lichess.
/// </summary>
public class LichessStream
{
    // Define a delegate for the event
    public delegate void GameUpdateEventHandler(object sender, JObject gameUpdate);

    private static readonly HttpClient _httpClient = new HttpClient();

    private static int LichessStreamCounter = 0;

    private readonly ILogger _logger;
    private HttpMethod method;

    private HttpRequestMessage request;
    private string requestUri = "https://lichess.org/api/bot/game/stream/gameId";

    private string StreamID = "";


    public LichessStream(string requestURL)
    {
        requestUri = requestURL;

        request = new HttpRequestMessage()
        {
            RequestUri = new Uri(requestUri)
        };
        method = HttpMethod.Get;

        var loggerFactory = LoggerFactory.Create(builder => builder
            .AddSpectreConsole());

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StreamID = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[new Random().Next(s.Length)]).ToArray());
        _logger = loggerFactory.CreateLogger("LichessStream_" + StreamID);
    }

    public LichessStream(HttpRequestMessage request, HttpMethod method)
    {
        this.request = request;
        this.method = method;

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        StreamID = new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[new Random().Next(s.Length)]).ToArray());
    }

    // Declare the event using the delegate
    public event GameUpdateEventHandler GameUpdateReceived;

    public async Task StreamGameAsync()
    {
        LichessStreamCounter++;

        if (LichessStreamCounter > 5)
        {
            _logger.LogWarning("There are already " + LichessStreamCounter +
                               " active streams. The maximum number of streams per IP on Lichess is 8.");
        }

        if (LichessStreamCounter >= 8)
        {
            while (LichessStreamCounter > 7)
            {
                _logger.LogError(
                    "The maximum of streams for lichess is reached. This stream won't be prepared until another stream is closed.");
            }
        }

        using (var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead))
        {
            response.EnsureSuccessStatusCode();

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        var json = JObject.Parse(line);
                        // Raise the event when a game update is received
                        GameUpdateReceived?.Invoke(this, json);
                        Console.WriteLine(line);
                    }
                }
            }
        }

        Console.WriteLine("Exiting Lichess Stream");
        LichessStreamCounter--;
    }
}