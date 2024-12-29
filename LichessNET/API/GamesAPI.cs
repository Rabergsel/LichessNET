using System.Text;
using LichessNET.Entities.Game;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    /// Retrieves a chess game using its unique identifier from the Lichess API.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to retrieve.</param>
    public async Task<Game> GetGameAsync(string gameId)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("game/export/" + gameId);

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        return Game.FromPgn(content);
    }

    /// <summary>
    /// Retrieves a list of chess games for a specified user from the Lichess API.
    /// </summary>
    /// <param name="username">The username of the player whose games are to be retrieved.</param>
    /// <param name="max">The maximum number of games to retrieve. Default is 10.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of games.</returns>
    public async Task<List<Game>> GetGamesAsync(string username, int max = 10)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("api/games/user/" + username,
            Tuple.Create("max", max.ToString()));

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var list = new List<Game>();

        var gamepgns = content.Split("\n\n\n");
        foreach (var gamepgn in gamepgns)
        {
            try
            {
                if (gamepgn.Length < 10) continue;
                list.Add(Game.FromPgn(gamepgn.Trim()));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to parse a pgn: " + gamepgn);
                throw;
            }
        }

        return list;
    }

    /// <summary>
    /// Retrieves multiple chess games from the Lichess API using a list of unique identifiers.
    /// </summary>
    /// <param name="ids">An array of unique game identifiers to retrieve.</param>
    /// <returns>A list of <see cref="Game"/> objects representing the retrieved chess games.</returns>
    public async Task<List<Game>> GetGamesAsync(params string[] ids)
    {
        var request = GetRequestScaffold("api/games/export/_ids");
        var idsJoined = string.Join(",", ids);
        request.Content = new StringContent(idsJoined, Encoding.UTF8, "text/plain");
        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadAsStringAsync();

        var list = new List<Game>();

        var gamepgns = content.Split("\n\n\n");
        foreach (var gamepgn in gamepgns)
        {
            try
            {
                if (gamepgn.Length < 10) continue;
                list.Add(Game.FromPgn(gamepgn.Trim()));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to parse a pgn: " + gamepgn);
                throw;
            }
        }

        return list;
    }

    /// <summary>
    /// Retrieves a list of chess games that have been imported to the Lichess platform.
    /// </summary>
    /// <returns>A list of imported chess games.</returns>
    public async Task<List<Game>> GetImportedGamesAsync()
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("api/games/export/import");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var list = new List<Game>();

        var gamepgns = content.Split("\n\n\n");
        foreach (var gamepgn in gamepgns)
        {
            try
            {
                if (gamepgn.Length < 10) continue;
                list.Add(Game.FromPgn(gamepgn.Trim()));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to parse a pgn: " + gamepgn);
                throw;
            }
        }

        return list;
    }

    /// <summary>
    /// Retrieves a list of chess games from a specified arena using the provided arena identifier.
    /// </summary>
    /// <param name="ArenaID">The unique identifier of the arena from which to retrieve the games.</param>
    /// <returns>A task representing the asynchronous operation that returns a list of games retrieved from the specified arena.</returns>
    public async Task<List<Game>> GetArenaGames(string ArenaID)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/tournament/{ArenaID}/games");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var list = new List<Game>();

        var gamepgns = content.Split("\n\n\n");
        foreach (var gamepgn in gamepgns)
        {
            try
            {
                if (gamepgn.Length < 10) continue;
                list.Add(Game.FromPgn(gamepgn.Trim()));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to parse a pgn: " + gamepgn);
                throw;
            }
        }

        return list;
    }

    /// <summary>
    /// Retrieves a list of games from a Swiss tournament using the Swiss ID from the Lichess API.
    /// </summary>
    /// <param name="SwissID">The unique identifier of the Swiss tournament to retrieve games from.</param>
    /// <returns>A task representing the asynchronous operation, with a list of Swiss tournament games as the result.</returns>
    public async Task<List<Game>> GetSwissGames(string SwissID)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/swiss/{SwissID}/games");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var list = new List<Game>();

        var gamepgns = content.Split("\n\n\n");
        foreach (var gamepgn in gamepgns)
        {
            try
            {
                if (gamepgn.Length < 10) continue;
                list.Add(Game.FromPgn(gamepgn.Trim()));
            }
            catch (Exception e)
            {
                _logger.LogWarning("Failed to parse a pgn: " + gamepgn);
                throw;
            }
        }

        return list;
    }

    /// <summary>
    /// Initializes a real-time stream of a chess game using its unique identifier from the Lichess API.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game to stream.</param>
    /// <returns>A GameStream object that provides updates as the game progresses.</returns>
    public async Task<GameStream> GetGameStreamAsync(string gameId)
    {
        var request = new HttpRequestMessage();
        request.RequestUri = new Uri("https://lichess.org/api/stream/game/" + gameId);
        return new GameStream(request, HttpMethod.Post);
    }

    /// <summary>
    /// Stream the games played between a list of users, in real time.
    /// Only games where both players are part of the list are included.
    /// The stream emits an event each time a game is started or finished.
    /// To also get all current ongoing games at the beginning of the stream, use the withCurrentGames flag. 
    /// </summary>
    /// <param name="UserIDs"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public async Task<GameStream> GetGameStreamByUserAsync(params string[] UserIDs)
    {
        if (UserIDs.Length > 300) throw new Exception("Lichess only allows up to 300 users to be tracked at once.");
        var request = new HttpRequestMessage();
        request.RequestUri = new Uri("https://lichess.org/api/stream/games-by-users");
        request.Content = new StringContent(string.Join(",", UserIDs), Encoding.UTF8, "text/plain");
        return new GameStream(request, HttpMethod.Post);
    }

    public async Task<GameStream> GetGameStreamByIDsAsync(params string[] GameIDs)
    {
        if (GameIDs.Length > 500) throw new Exception("Lichess only allows up to 500 games to be tracked at once.");
        var request = new HttpRequestMessage();
        request.RequestUri = new Uri("https://lichess.org/api/stream/streamlichessnet}");
        request.Content = new StringContent(string.Join(",", GameIDs), Encoding.UTF8, "text/plain");
        return new GameStream(request, HttpMethod.Post);
    }

    /// <summary>
    /// Fetches the ongoing games for the current user.
    /// </summary>
    /// <param name="maxGames">The maximum number of ongoing games to fetch (default is 9, max 50).</param>
    /// <returns>A list of OngoingGame objects representing the current ongoing games.</returns>
    public async Task<List<OngoingGame>> GetOngoingGamesAsync(int maxGames = 9)
    {
        _ratelimitController.Consume("api/account", false);

        if (maxGames < 1 || maxGames > 50)
            throw new ArgumentOutOfRangeException(nameof(maxGames), "The number of games must be between 1 and 50.");

        var request = GetRequestScaffold("api/account/playing", Tuple.Create("nb", maxGames.ToString()));
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();
        var jobj = JsonConvert.DeserializeObject<JObject>(content);
        return jobj["nowPlaying"].ToObject<List<OngoingGame>>();
    }

    /// <summary>
    /// Imports a game from PGN.
    /// </summary>
    /// <param name="pgn">The PGN of the game to import.</param>
    /// <returns>A task representing the asynchronous operation, containing the import game response.</returns>
    public async Task<string> ImportGameAsync(string pgn)
    {
        _ratelimitController.Consume();

        var endpoint = "api/import";
        var request = GetRequestScaffold(endpoint);

        var formData = new Dictionary<string, string>
        {
            { "pgn", pgn }
        };

        request.Content = new FormUrlEncodedContent(formData);

        var response = await SendRequest(request, HttpMethod.Post);
        var content = await response.Content.ReadAsStringAsync();
        var obj = JsonConvert.DeserializeObject<dynamic>(content);
        var importGameID = obj["id"].ToString();
        return importGameID;
    }
}