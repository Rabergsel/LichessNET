﻿using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Social;
using LichessNET.Entities.Stats;
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
    public async Task<UserRealTimeStatus> GetRealTimeUserStatus(string id, bool withSignal = false,
        bool withGameIds = false, bool withGameMetas = false)
    {
        return (await GetRealTimeUserStatus(new List<string> { id }, withSignal, withGameIds, withGameMetas))[0];
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
    public async Task<List<UserRealTimeStatus>> GetRealTimeUserStatus(IEnumerable<string> ids, bool withSignal = false,
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
}