using BrowserGame.Models;

namespace BrowserGame.Utils;

public interface IPlayerPositioner
{
    void MovePlayer(PlayerModel player, MapModel map, long delta, double playerHitboxRadius);

    /// <summary>
    ///     change player's position to a center of random, traversable field
    /// </summary>
    /// <param name="player">player whose position is to be changed</param>
    /// <param name="map">map the player is moving on</param>
    void RandomPlayerPosition(PlayerModel player, MapModel map);
}
