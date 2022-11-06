using BrowserGameBackend.Models;

namespace BrowserGameBackend.Data;

public interface IGameData
{
    /// <summary>
    /// Returns list of all games
    /// </summary>
    /// <returns></returns>
    List<GameModel> GetAll();

    /// <summary>
    /// Return game with specified id or null, if such game does not exist
    /// </summary>
    /// <param name="id">Id of game</param>
    /// <returns></returns>
    GameModel? GetById(Guid id);

    /// <summary>
    /// Return game played by player with specified id or null, if such game does not exist
    /// </summary>
    /// <param name="id">Id of player</param>
    /// <returns></returns>
    GameModel? GetByPlayerId(string id);
    /// <summary>
    /// Returns player with specified id or null, if such player does not exist
    /// </summary>
    /// <param name="Id">Id of player</param>
    /// <returns></returns>
    PlayerModel? GetPlayerById(string Id);
    bool AddGame(GameModel game);

    /// <summary>
    /// Adds player to a game with specified id
    /// </summary>
    /// <param name="player">Player to add to list of players</param>
    /// <param name="gameId">Id of a game</param>
    /// <returns>0 on success, -1 if game with given id does not exist, -2 if number of players reached maximum, -3 if player is in another game</returns>
    int AddPlayer(PlayerModel player, Guid gameId);

    /// <summary>
    /// Delete game with specified id
    /// </summary>
    /// <param name="id">Id of the game to remove</param>
    /// <returns>0 on successful deletion, negative number on failure</returns>
    int DeleteGame(Guid id);

    /// <summary>
    /// Delete player with specified id from game with specified id
    /// </summary>
    /// <param name="playerId">Id of the player to delete</param>
    /// <returns>0 on success, negative number in other case</returns>
    int DeletePlayer(string playerId);
}
