using LichessNET.Entities.Account.Performance;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Social;
using LichessNET.Entities.Social.Stream;
using LichessNET.Entities.Stats;
using LichessNET.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    ///     Retrieves the real-time status of a specified user on Lichess.
    /// </summary>
    /// <param name="id">The ID of the user whose real-time status is to be retrieved.</param>
    /// <param name="withSignal">Optional parameter to include the connectivity signal status of the user.</param>
    /// <param name="withGameIds">Optional parameter to include the IDs of the games the user is playing.</param>
    /// <param name="withGameMetas">Optional parameter to include meta information about the games the user is involved in.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, containing the real-time status of the user.
    /// </returns>
    public async Task<UserRealTimeStatus> GetRealTimeUserStatusAsync(string id, bool withSignal = false,
        bool withGameIds = false, bool withGameMetas = false)
    {
        return (await GetRealTimeUserStatusAsync(new List<string> { id }, withSignal, withGameIds, withGameMetas))[0];
    }

    /// <summary>
    ///     Retrieves the real-time status of specified users on Lichess.
    /// </summary>
    /// <param name="ids">A collection of user IDs whose real-time statuses are to be retrieved.</param>
    /// <param name="withSignal">Optional parameter to include the connectivity signal status of users.</param>
    /// <param name="withGameIds">Optional parameter to include the IDs of the games users are playing.</param>
    /// <param name="withGameMetas">Optional parameter to include meta information about the games users are involved in.</param>
    /// <returns>
    ///     A task representing the asynchronous operation, containing a list of user real-time statuses.
    /// </returns>
    public async Task<List<UserRealTimeStatus>> GetRealTimeUserStatusAsync(IEnumerable<string> ids,
        bool withSignal = false,
        bool withGameIds = false, bool withGameMetas = false)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("api/users/status",
            Tuple.Create("ids", string.Join(",", ids)),
            Tuple.Create("withSignal", withSignal.ToString().ToLower()),
            Tuple.Create("withGameIds", withGameIds.ToString().ToLower()),
            Tuple.Create("withGameMetas", withGameMetas.ToString().ToLower())
        );

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var userStatuses = JsonConvert.DeserializeObject<List<UserRealTimeStatus>>(content);
        return userStatuses;
    }

    /// <summary>
    /// Asynchronously retrieves the top players' leaderboard from Lichess for a specified number of players and game mode.
    /// </summary>
    /// <param name="nb">The number of top players to be retrieved from the leaderboard.</param>
    /// <param name="perfType">The game mode for which the leaderboard is to be retrieved.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing a list of top players in the specified game mode.
    /// </returns>
    public async Task<List<LichessUser>> GetLeaderboardAsync(int nb, Gamemode perfType)
    {
        _ratelimitController.Consume();

        var users = new List<LichessUser>();
        var endpoint = $"api/player/top/{nb}/{perfType.ToString().ToLower()}";

        var request = GetRequestScaffold(endpoint);
        try
        {
            var response = await SendRequest(request);
            var content = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(content);

            foreach (var userJson in json["users"])
            {
                var user = new LichessUser
                {
                    Id = userJson["id"]?.ToString(),
                    Username = userJson["username"]?.ToString(),
                };

                var perfs = userJson["perfs"]?.ToObject<Dictionary<string, dynamic>>();
                if (perfs != null && perfs.ContainsKey(perfType.ToString().ToLower()))
                {
                    user.Ratings = new Dictionary<Gamemode, GamemodeStats>
                    {
                        {
                            perfType,
                            new GamemodeStats
                            {
                                Rating = (int)(perfs[perfType.ToString().ToLower()]["rating"] ?? 0),
                                Progress = (int)(perfs[perfType.ToString().ToLower()]["progress"] ?? 0)
                            }
                        }
                    };
                }

                users.Add(user);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception occurred while fetching leaderboard for {perfType}: {ex.Message}");
        }

        return users;
    }

    public async Task<Dictionary<Gamemode, List<LichessUser>>> GetAllLeaderboardsAsync()
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold("api/player");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var json = JObject.Parse(content);
        var leaderboards = new Dictionary<Gamemode, List<LichessUser>>();

        foreach (var property in json.Properties())
        {
            if (Enum.TryParse(property.Name, true, out Gamemode gamemode))
            {
                var users = property.Value.ToObject<List<LichessUser>>();
                leaderboards[gamemode] = users ?? new List<LichessUser>();
            }
        }

        return leaderboards;
    }

    /// <summary>
    /// Retrieves the cross table for two specified users, summarizing the results of their games.
    /// </summary>
    /// <param name="user1">The ID of the first user in the cross table.</param>
    /// <param name="user2">The ID of the second user in the cross table.</param>
    /// <param name="includeMatchup">Optional parameter to include current matchup details if available.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the cross table data for the two users.
    /// </returns>
    public async Task<CrossTable> GetCrossTableAsync(string user1, string user2, bool includeMatchup = false)
    {
        //NOTE: The problem is, that lichess will provide matchup if the query parameter is provided.
        //This means, that we cannot just use the query parameter, but we have to check if the matchup is provided in the response.

        _ratelimitController.Consume();

        var endpoint = $"api/crosstable/{user1}/{user2}";
        if (includeMatchup)
        {
            endpoint += "?matchup=true";
        }

        var request = GetRequestScaffold(endpoint);


        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);

        var crossTable = new CrossTable
        {
            TotalGames = json["nbGames"]?.ToObject<int>() ?? 0,
            Scores = json["users"]?.ToObject<Dictionary<string, double>>() ?? new Dictionary<string, double>()
        };

        // Include matchup if requested
        if (includeMatchup && json["matchup"] != null)
        {
            crossTable.CurrentMatchup = new Matchup
            {
                TotalGames = json["matchup"]["nbGames"]?.ToObject<int>() ?? 0,
                Scores = json["matchup"]["users"]?.ToObject<Dictionary<string, int>>() ?? new Dictionary<string, int>()
            };
        }

        return crossTable;
    }

    /// <summary>
    /// Retrieves the profile of a specified user on Lichess.
    /// </summary>
    /// <param name="username">The username of the user whose profile is to be retrieved.</param>
    /// <returns>
    /// A task representing the asynchronous operation, containing the user's profile.
    /// </returns>
    public async Task<LichessUser> GetUserProfile(string username)
    {
        //NOTE: The problem is, that lichess will provide matchup if the query parameter is provided.
        //This means, that we cannot just use the query parameter, but we have to check if the matchup is provided in the response.

        _ratelimitController.Consume();

        var endpoint = $"api/user/{username}";
        var request = GetRequestScaffold(endpoint);
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();
        var json = JObject.Parse(content);


        LichessUser user = json.ToObject<LichessUser>();
        user.Ratings = LichessUser.DeserializeRatings(json["perfs"]);


        return user;
    }

    public async Task<Dictionary<Gamemode, List<RatingDataPoint>>> GetRatingHistory(string username)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/user/{username}/rating-history");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var json = JArray.Parse(content);
        var ratingHistory = new Dictionary<Gamemode, List<RatingDataPoint>>();

        foreach (var item in json)
        {
            if (Enum.TryParse(item["name"]?.ToString(), true, out Gamemode gamemode))
            {
                var points = item["points"].ToObject<List<List<int>>>();
                var dataPoints = points.Select(p => new RatingDataPoint(p[0], p[1], p[2], p[3])).ToList();
                ratingHistory[gamemode] = dataPoints;
            }
        }

        return ratingHistory;
    }

    /// <summary>
    /// Gets the performance stats of a specified user on Lichess.
    /// </summary>
    /// <param name="username">The user to load</param>
    /// <param name="perf">The gamemode of which to load the performance</param>
    /// <returns></returns>
    public async Task<PerformanceStats> GetUserPerformanceStatsAsync(string username, Gamemode gamemode)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/user/{username}/perf/{gamemode.GetEnumMemberValue()}");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var performanceStatsResponse = JsonConvert.DeserializeObject<PerformanceStats>(content);

        return performanceStatsResponse;
    }

    /// <summary>
    /// Returns all streamers currently being live
    /// </summary>
    /// <returns></returns>
    public async Task<List<LiveStreamer>> GetAllLiveStreamers()
    {
        _ratelimitController.Consume("api/streamer/live", false);

        var request = GetRequestScaffold("api/streamer/live");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var liveStreamers = JsonConvert.DeserializeObject<List<LiveStreamer>>(content);
        return liveStreamers;
    }

    /// <summary>
    /// Adds a note for the specified user
    /// </summary>
    /// <param name="username">User to add the note to.</param>
    /// <param name="text">The note</param>
    /// <returns>A boolean whether the post was successful.</returns>
    public async Task<bool> AddUserNoteAsync(string username, string text)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/user/{username}/note";
        var request = GetRequestScaffold(endpoint);

        var parameters = new Dictionary<string, string>
        {
            { "text", text }
        };

        request.Content = new FormUrlEncodedContent(parameters);
        var response = await SendRequest(request, HttpMethod.Post);
        return response.Content.ReadAsStringAsync().Result.Contains("true");
    }

    /// <summary>
    /// Gets the note from the authorized user for the specified user
    /// </summary>
    /// <param name="username">The user to get the notes for</param>
    /// <returns>A list of notes</returns>
    public async Task<List<Note>> GetUserNotesAsync(string username)
    {
        _ratelimitController.Consume();

        var endpoint = $"api/user/{username}/note";
        var request = GetRequestScaffold(endpoint);

        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var notes = JsonConvert.DeserializeObject<List<Note>>(content);
        return notes;
    }


    /// <summary>
    /// Retrieves the list of users followed by the logged-in user.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the list of followed users.</returns>
    public async Task<List<LichessUser>> GetFollowedUsersAsync()
    {
        _ratelimitController.Consume("api/rel/following", false);

        var request = GetRequestScaffold("api/rel/following");
        var response = await SendRequest(request);
        var content = await response.Content.ReadAsStringAsync();

        var followedUsers = new List<LichessUser>();
        using (var reader = new StringReader(content))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                var user = JsonConvert.DeserializeObject<LichessUser>(line);
                followedUsers.Add(user);
            }
        }

        return followedUsers;
    }
}