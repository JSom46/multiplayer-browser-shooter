using BrowserGame.Models;

namespace BrowserGame.Data;

public class GameData : IGameData
{
    private readonly SynchronizedCollection<GameModel> _games;
    private readonly Dictionary<string, GameModel> _playerIdGame;
    private readonly Dictionary<string, PlayerModel> _playerIdPlayer;

    public GameData()
    {
        _games = new SynchronizedCollection<GameModel>();
        _playerIdGame = new Dictionary<string, GameModel>();
        _playerIdPlayer = new Dictionary<string, PlayerModel>();
    }

    public SynchronizedCollection<GameModel> GetAll()
    {
        return _games;
    }

    /// <param name="id"></param>
    /// <returns>Return game with specified id or null, if such game does not exist</returns>
    public GameModel? GetById(Guid id)
    {
        return _games.FirstOrDefault(e => e.Id == id);
    }

    /// <param name="id"></param>
    /// <returns>Return game played by player with specified id or null, if such game does not exist</returns>
    public GameModel? GetByPlayerId(string id)
    {
        _playerIdGame.TryGetValue(id, out var game);
        return game;
    }

    /// <param name="id"></param>
    /// <returns>Returns player with specified id or null, if such player does not exist</returns>
    public PlayerModel? GetPlayerById(string id)
    {
        _playerIdPlayer.TryGetValue(id, out var player);
        return player;
    }

    /// <summary>
    ///     Adds game
    /// </summary>
    /// <param name="game"></param>
    /// <returns>true on success, false otherwise</returns>
    public bool AddGame(GameModel game)
    {
        if (_games.FirstOrDefault(g => g.Id == game.Id) != null) return false;

        _games.Add(game);
        return true;
    }

    /// <summary>
    ///     Adds player to a game with specified id
    /// </summary>
    /// <param name="player">Player to add to list of players</param>
    /// <param name="gameId">Id of a game</param>
    /// <returns>
    ///     0 on success, -1 if game with given id does not exist, -2 if number of players reached maximum, -3 if player
    ///     is in another game
    /// </returns>
    public int AddPlayer(PlayerModel player, Guid gameId)
    {
        var game = _games.FirstOrDefault(g => g.Id == gameId);

        if (game == null)
            // game does not exist
            return -1;

        if (game.MaxPlayers <= game.Players.Count)
            // no more room for new players
            return -2;

        if (!_playerIdGame.TryAdd(player.Id, game))
            // player is in another game
            return -3;

        if (!_playerIdPlayer.TryAdd(player.Id, player))
            // ???
            return -4;

        game.Players.Add(player);
        return 0;
    }

    /// <summary>
    ///     Delete game with specified id
    /// </summary>
    /// <param name="id">Id of the game to remove</param>
    /// <returns>0 on successful deletion, negative number on failure</returns>
    public int DeleteGame(Guid id)
    {
        var game = _games.FirstOrDefault(e => e.Id == id);

        if (game == null)
            // game does not exist
            return -1;

        foreach (var playerModel in game.Players)
        {
            _playerIdGame.Remove(playerModel.Id);
            _playerIdPlayer.Remove(playerModel.Id);
        }

        return _games.Remove(game) ? 0 : -2;
    }

    /// <summary>
    ///     Delete player with specified id
    /// </summary>
    /// <param name="playerId">Id of the player to delete</param>
    /// <returns>0 on success, negative number in other case</returns>
    public int DeletePlayer(string playerId)
    {
        if (!_playerIdGame.TryGetValue(playerId, out var game)) return -1;

        if (!_playerIdGame.Remove(playerId)) return -3;

        if (!_playerIdPlayer.Remove(playerId)) return -4;

        if (!game.Players.Remove(game.Players.First(p => p.Id == playerId))) return -5;

        return 0;
    }
}