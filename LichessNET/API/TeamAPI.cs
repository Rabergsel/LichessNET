﻿using LichessNET.Entities.Teams;
using Newtonsoft.Json;

namespace LichessNET.API;

public partial class LichessApiClient
{
    /// <summary>
    ///     Get the team with the specified ID
    /// </summary>
    /// <param name="teamId">The ID of the team</param>
    /// <returns>The team with the specified ID</returns>
    public async Task<LichessTeam> GetTeamAsync(string teamId)
    {
        _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/team/{teamId}");
        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var team = JsonConvert.DeserializeObject<LichessTeam>(content);

        return team;
    }

    /// <summary>
    /// Get the list of teams that the specified user is in.
    /// </summary>
    /// <param name="username">The username of the user</param>
    /// <returns>A list of teams that the specified user is in</returns>
    public async Task<List<LichessTeam>> GetTeamOfUserAsync(string username)
    {
        _ratelimitController.Consume();

        var request = GetRequestScaffold($"api/team/of/{username}");
        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var teams = JsonConvert.DeserializeObject<List<LichessTeam>>(content);

        return teams;
    }

    /// <summary>
    /// This funciton gets the members of a team, in chronoclogical order of joining the team. (Latest first),
    /// with up to 5000 members.
    /// </summary>
    /// <param name="teamId">ID of the Team</param>
    /// <returns></returns>
    public async Task<List<TeamMember>> GetTeamMembersAsync(string teamId)
    {
        _ratelimitController.Consume();
        var request = GetRequestScaffold($"api/team/{teamId}/users");

        var response = await SendRequest(request);

        var content = await response.Content.ReadAsStringAsync();
        var members = JsonConvert.DeserializeObject<List<TeamMember>>(content);

        return members;
    }
}