using LichessNET.Entities.Enumerations;
using LichessNET.Entities.Social;

namespace LichessNET.Entities.Game;

/// <summary>
/// Represents a player in a chess game, including their name, title, and rating.
/// This class will be used when parsing PGN Files
/// </summary>
public class GamePlayer
{
    public string Name { get; set; }
    public Title Title { get; set; } = Title.None;
    public int Rating { get; set; }

    public static implicit operator Opponent(GamePlayer player)
    {
        Opponent opponent = new Opponent();
        opponent.Username = player.Name;
        opponent.Rating = player.Rating;
        return opponent;
    }

    public static implicit operator LichessUser(GamePlayer player)
    {
        LichessUser user = new LichessUser();
        user.Username = player.Name;
        user.Title = player.Title;
        return user;
    }
}