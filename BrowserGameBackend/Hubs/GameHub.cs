using BrowserGame.Data;
using BrowserGame.Dtos;
using BrowserGame.Models;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGame.Hubs
{
    public class GameHub : Hub
    {
        private readonly IMapData _maps;
        private readonly IGameData _games;

        public GameHub(IMapData maps, IGameData games)
        {
            _maps = maps;
            _games = games;
        }

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override async Task<Task> OnDisconnectedAsync(Exception? exception)
        {
            // check if player was in any game
            var game = _games.GetByPlayerId(Context.ConnectionId);
            if (game == null)
            {
                return base.OnDisconnectedAsync(exception);
            }

            // delete player from the game
            _games.DeletePlayer(Context.ConnectionId);

            // if player was the only one in the game, delete game
            if (game.Players.Count == 0)
            {
                _games.DeleteGame(game.Id);
            }

            await Clients.Group(game.Id.ToString()).SendAsync("playerLeft", Context.ConnectionId);
            return base.OnDisconnectedAsync(exception);
        }

        public async Task CreateRoom(CreateGameData data)
        {
            // check if specified map exists
            if (_maps.GetByName(data.Map) == null)
            {
                await Clients.Caller.SendAsync("createError");
                return;
            }

            // create game object and add it to games' list
            var gameId = Guid.NewGuid();
            var game = new GameModel()
            { 
                Id = gameId,
                MapName = data.Map,
                MaxPlayers = 32,
                Name = data.GameName
            };
            _games.AddGame(game);

            // add player to his game
            _games.AddPlayer(new PlayerModel()
            {
                Id = Context.ConnectionId,
                Name = data.PlayerName,
                X = 0,
                Y = 0,
                LastStateUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            }, gameId);

            // create game's group and add player to it
            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id.ToString());

            // send to player response with game's state
            await Clients.Caller.SendAsync("gameJoined", new FullGameState()
            {
                Players = game.Players.Select(p => new FullPlayerState()
                {
                    Deaths = p.Deaths,
                    Id = p.Id,
                    Kills = p.Kills,
                    MovementSpeed = p.MovementSpeed,
                    Name = p.Name,
                    ProjectilesSpeed = p.ProjectilesSpeed,
                    R = p.R,
                    X = p.X,
                    Y = p.Y,
                    IsAlive = p.IsAlive
                }),
                Projectiles = new List<ProjectileModel>()
            });
        }

        public async Task JoinRoom(string gameIdString, string playerName)
        {
            Guid gameId;
            try
            {
                gameId = Guid.Parse(gameIdString);
            }
            catch (FormatException e)
            {
                await Clients.Caller.SendAsync("joinError");
                return;
            }
            
            // check if player is not in another game
            if (_games.GetByPlayerId(Context.ConnectionId) != null)
            {
                await Clients.Caller.SendAsync("joinError");
                return;
            }

            var game = _games.GetById(gameId);

            // check if game exists
            if (game == null)
            {
                await Clients.Caller.SendAsync("joinError");
                return;
            }

            // check if there's a room for a player
            if (game.Players.Count >= game.MaxPlayers)
            {
                await Clients.Caller.SendAsync("joinError");
                return;
            }

            // create player object and add it to players' list
            var player = new PlayerModel()
            {
                Id = Context.ConnectionId,
                Name = playerName,
                X = 0,
                Y = 0,
                LastStateUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            _games.AddPlayer(player, gameId);

            // inform other players about new player
            await Clients.Group(gameId.ToString()).SendAsync("playerJoined", new FullPlayerState()
            {
                Deaths = player.Deaths,
                Id = player.Id,
                Kills = player.Kills,
                MovementSpeed = player.MovementSpeed,
                Name = player.Name,
                ProjectilesSpeed = player.ProjectilesSpeed,
                R = player.R,
                X = player.X,
                Y = player.Y,
                IsAlive = player.IsAlive
            });

            // add player to hub's room
            await Groups.AddToGroupAsync(Context.ConnectionId, game.Id.ToString());

            await Clients.Caller.SendAsync("gameJoined", new FullGameState()
            {
                Players = game.Players.Select(p => new FullPlayerState()
                {
                    Deaths = p.Deaths,
                    Id = p.Id,
                    Kills = p.Kills,
                    MovementSpeed = p.MovementSpeed,
                    Name = p.Name,
                    ProjectilesSpeed = p.ProjectilesSpeed,
                    R = p.R,
                    X = p.X,
                    Y = p.Y,
                    IsAlive = p.IsAlive
                }),
                Projectiles = game.Projectiles
            });
        }

        public async Task LeaveRoom(string gameIdString)
        {
            Guid gameId;
            try
            {
                gameId = Guid.Parse(gameIdString);
            }
            catch (FormatException e)
            {
                await Clients.Caller.SendAsync("leaveError");
                return;
            }
            
            var game = _games.GetById(gameId);

            // check if game exists
            if (game == null)
            {
                await Clients.Caller.SendAsync("leaveError");
                return;
            }

            // remove player from game list and inform other players about leaving
            if (_games.DeletePlayer(Context.ConnectionId) == 0)
            {
                await Clients.Group(gameIdString).SendAsync("playerLeft", Context.ConnectionId);
            }

            // if room is empty, delete it
            if (game.Players.Count == 0)
            {
                _games.DeleteGame(gameId);
            }

            await Clients.Caller.SendAsync("gameLeft");
        }

        public void UpdateState(int movementDirection, double rotation, int? action)
        {
            var player = _games.GetPlayerById(Context.ConnectionId);

            if (player == null)
            {
                return;
            }

            try
            {
                player.Moves.Enqueue(new PlayerActionModel(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    movementDirection, rotation, action));
            }
            catch(ArgumentException e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                Console.WriteLine(player.Moves.Count);
            }
        }
    }
}
