using LichessNET.Entities.Social;

namespace LichessNET.Entities.Game;

/// <summary>
/// All opponent data sent with ongoing games
/// </summary>
public class Opponent
{
    public string Id { get; set; }
    public int Rating { get; set; }
    public string Username { get; set; }

    /// <summary>
    /// Converts an opponent to a GamePlayer
    /// </summary>
    /// <param name="opponent">Opponent to convert</param>
    /// <returns>A GamePlayer object that contains all information that the Opponent carries</returns>
    public static implicit operator GamePlayer(Opponent opponent)
    {
        GamePlayer player = new GamePlayer();
        player.Name = opponent.Id;
        player.Rating = opponent.Rating;
        return player;
    }

    /// <summary>
    /// Converts an opponent to a LichessUser
    /// </summary>
    /// <param name="opponent">Opponent to convert</param>
    /// <returns>A LichessUser object that contains all information that the Opponent carries</returns>
    public static implicit operator LichessUser(Opponent opponent)
    {
        LichessUser user = new LichessUser();
        user.Id = opponent.Id;
        user.Username = opponent.Username;
        return user;
    }
}