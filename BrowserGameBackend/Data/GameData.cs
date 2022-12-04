using BrowserGame.Models;

namespace BrowserGame.Data
{
    public class GameData : IGameData
    {
        private SynchronizedCollection<GameModel> Games;
        private Dictionary<string, GameModel> PlayerIdGame;
        private Dictionary<string, PlayerModel> PlayerIdPlayer;

        public GameData()
        {
            Games = new SynchronizedCollection<GameModel>();
            PlayerIdGame = new Dictionary<string, GameModel>();
            PlayerIdPlayer = new Dictionary<string, PlayerModel>();

            var game = new GameModel()
            {
                Id = Guid.NewGuid(),
                MapName = "map1",
                MaxPlayers = 69,
                Name = "ultimate game of absolute destruction"
            };

            game.Projectiles.Add(new ProjectileModel()
            {
                Id = game.NextProjectileId++,
                PId = "xd",
                SX = 0.3,
                SY = 0.4,
                X = 0,
                Y = 0
            });

            this.AddGame(game);
            this.AddPlayer(new PlayerModel()
            {
                Id = "xd",
                X = 69.69,
                Y = 21.37,
                Name = "dominator",
                LastStateUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, game.Id);
        }

        public SynchronizedCollection<GameModel> GetAll()
        {
            return Games;
        }

        /// <param name="id"></param>
        /// <returns>Return game with specified id or null, if such game does not exist</returns>
        public GameModel? GetById(Guid id)
        {
            return Games.FirstOrDefault(e => e.Id == id);
        }

        /// <param name="id"></param>
        /// <returns>Return game played by player with specified id or null, if such game does not exist</returns>
        public GameModel? GetByPlayerId(string id)
        {
            PlayerIdGame.TryGetValue(id, out var game);
            return game;
        }

        /// <param name="id"></param>
        /// <returns>Returns player with specified id or null, if such player does not exist</returns>
        public PlayerModel? GetPlayerById(string id)
        {
            PlayerIdPlayer.TryGetValue(id, out var player);
            return player;
        }

        /// <summary>
        /// Adds game
        /// </summary>
        /// <param name="game"></param>
        /// <returns>true on success, false otherwise</returns>
        public bool AddGame(GameModel game)
        {
            if (Games.FirstOrDefault(g => g.Id == game.Id) != null)
            {
                return false;
            }

            Games.Add(game);
            return true;
        }

        /// <summary>
        /// Adds player to a game with specified id
        /// </summary>
        /// <param name="player">Player to add to list of players</param>
        /// <param name="gameId">Id of a game</param>
        /// <returns>0 on success, -1 if game with given id does not exist, -2 if number of players reached maximum, -3 if player is in another game</returns>
        public int AddPlayer(PlayerModel player, Guid gameId)
        {
            var game = Games.FirstOrDefault(g => g.Id == gameId);

            if (game == null)
            {
                // game does not exist
                return -1;
            }

            if (game.MaxPlayers <= game.Players.Count)
            {
                // no more room for new players
                return -2;
            }

            if (!PlayerIdGame.TryAdd(player.Id, game))
            {
                // player is in another game
                return -3;
            }

            if (!PlayerIdPlayer.TryAdd(player.Id, player))
            {
                // ???
                return -4;
            }

            game.Players.Add(player);
            return 0;
        }

        /// <summary>
        /// Delete game with specified id
        /// </summary>
        /// <param name="id">Id of the game to remove</param>
        /// <returns>0 on successful deletion, negative number on failure</returns>
        public int DeleteGame(Guid id)
        {
            var game = Games.FirstOrDefault(e => e.Id == id);

            if (game == null)
            {
                // game does not exist
                return -1;
            }

            foreach (var playerModel in game.Players)
            {
                PlayerIdGame.Remove(playerModel.Id);
                PlayerIdPlayer.Remove(playerModel.Id);
            }

            return Games.Remove(game) ? 0 : -2;
        }

        /// <summary>
        /// Delete player with specified id
        /// </summary>
        /// <param name="playerId">Id of the player to delete</param>
        /// <returns>0 on success, negative number in other case</returns>
        public int DeletePlayer(string playerId)
        {
            if (!PlayerIdGame.TryGetValue(playerId, out var game))
            {
                return -1;
            }

            if (!PlayerIdGame.Remove(playerId))
            {
                return -3;
            }

            if (!PlayerIdPlayer.Remove(playerId))
            {
                return -4;
            }

            if (!game.Players.Remove(game.Players.First(p => p.Id == playerId)))
            {
                return -5;
            }

            return 0;
        }
    }
}