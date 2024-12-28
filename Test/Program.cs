using System.Text.Json;
using LichessNET.API;
using LichessNET.Database;
using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Game;

var client = new LichessApiClient(File.ReadAllText("token.txt"));
var database = new DatabaseClient();

var popularteams = await client.GetPopularTeamsAsync(2);
foreach (var team in popularteams)
{
    Console.WriteLine(team.Name);
}

