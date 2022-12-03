using BrowserGame.Models;

namespace BrowserGame.Data;

public interface IGameData
{
    List<GameModel> GetAll();

    /// <param name="id"></param>
    /// <returns>Return game with specified id or null, if such game does not exist</returns>
    GameModel? GetById(Guid id);

    /// <param name="id"></param>
    /// <returns>Return game played by player with specified id or null, if such game does not exist</returns>
    GameModel? GetByPlayerId(string id);

    /// <param name="Id"></param>
    /// <returns>Returns player with specified id or null, if such player does not exist</returns>
    PlayerModel? GetPlayerById(string Id);

    /// <summary>
    /// Adds game
    /// </summary>
    /// <param name="game"></param>
    /// <returns>true on success, false otherwise</returns>
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
    /// Delete player with specified id
    /// </summary>
    /// <param name="playerId">Id of the player to delete</param>
    /// <returns>0 on success, negative number in other case</returns>
    int DeletePlayer(string playerId);
}