using BrowserGameBackend.Data;
using BrowserGameBackend.Dtos;
using BrowserGameBackend.Hubs;
using BrowserGameBackend.Models;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGameBackend
{
    public class GameUpdateService : BackgroundService
    {
        private readonly IGameData _gameData;
        private readonly IMapData _mapData;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly PeriodicTimer _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(33));
        private long _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private readonly double _playerHitboxRadius = 0.4;
        private readonly double _projectileHitboxRadius = 0.25;

        public GameUpdateService(IGameData gameData, IMapData mapData, IHubContext<GameHub> hubContext)
        {
            _gameData = gameData;
            _mapData = mapData;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                // update state of each game
                foreach (var game in _gameData.GetAll())
                {
                    var map = _mapData.GetByName(game.MapName);
                    // number of milliseconds since last tick
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var deltaTime = now - _lastTick;
                    var deletedProjectiles = new List<int>();
                    var newProjectiles = new List<ProjectileModel>();

                    // update state of each projectile in game
                    for (var i = game.Projectiles.Count - 1; i >= 0; --i)
                    {
                        game.Projectiles[i].X += game.Projectiles[i].SX * deltaTime;
                        game.Projectiles[i].Y += game.Projectiles[i].SY * deltaTime;

                        // remove projectile if it is outside of map boundries
                        if (game.Projectiles[i].X > map.Width * map.TileWidth ||
                            game.Projectiles[i].Y > map.Height * map.TileHeight || 
                            game.Projectiles[i].X < 0 || game.Projectiles[i].Y < 0)
                        {
                            deletedProjectiles.Add(game.Projectiles[i].Id);
                            game.Projectiles.Remove(game.Projectiles[i]);
                            continue;
                        }

                        try
                        {
                            // remove projectile if it hits an obstacle
                            if (!map.CanBeShotThrough[(int)Math.Floor(game.Projectiles[i].Y / map.TileHeight)][
                                    (int)Math.Floor(game.Projectiles[i].X / map.TileWidth)])
                            {
                                game.Projectiles.Remove(game.Projectiles[i]);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("mapy: map");
                            Console.WriteLine("y: " + (int)Math.Floor(game.Projectiles[i].X / map.TileWidth) + " x: " + (int)Math.Floor(game.Projectiles[i].Y / map.TileHeight));
                            Console.WriteLine(e.ToString());
                        }
                        
                    }
                    
                    // update state of each player in game
                    foreach (var player in game.Players)
                    {
                        var movementDirection = player.LastMovementDirection;
                        var lastActionTimeStamp = _lastTick;

                        //foreach (var action in player.Moves)
                        while(player.Moves.TryDequeue(out var action))
                        {
                            var timeSinceLastAction = action.Timestamp - lastActionTimeStamp;

                            // move player TODO needs to be changed - player is not a single point
                            var newX = player.X; 
                            var newY = player.Y;
                            switch (movementDirection)
                            {
                                // upper-left
                                case 0:
                                {
                                    newX -= player.MovementSpeed * timeSinceLastAction;
                                    newY -= player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // up
                                case 1:
                                {
                                    newY -= player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // upper-right
                                case 2:
                                {
                                    newX += player.MovementSpeed * timeSinceLastAction;
                                    newY -= player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // left
                                case 3:
                                {
                                    newX -= player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // right
                                case 5:
                                {
                                    newX += player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // bottom-left
                                case 6:
                                {
                                    newX -= player.MovementSpeed * timeSinceLastAction;
                                    newY += player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // down
                                case 7:
                                {
                                    newY += player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // bottom-right
                                case 8:
                                {
                                    newX += player.MovementSpeed * timeSinceLastAction;
                                    newY += player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                            }
                            /*if (map.IsTraversable[(int)Math.Floor(newX / map.TileWidth)][(int)Math.Floor(newY / map.TileHeight)])
                            {
                                player.X = newX;
                                player.Y = newY;
                            }*/

                            // change movement direction if it was specified in action
                            if (action.MovementDirection != null)
                            {
                                movementDirection = (int)action.MovementDirection;
                            }

                            // change player's rotation if it was specified in action
                            if (action.R != null)
                            {
                                player.R = (double)action.R;
                            }

                            // if other action was specified, execute it
                            if (action.Action == 0) {
                                // action == shoot
                                var projectile = new ProjectileModel()
                                {
                                    Id = game.NextProjectileId++,
                                    PId = player.Id,
                                    X = player.X,
                                    Y = player.Y,
                                    SX = player.ProjectilesSpeed * Math.Sin(player.R),
                                    SY = player.ProjectilesSpeed * -Math.Cos(player.R)
                                };
                                game.Projectiles.Add(projectile);
                                newProjectiles.Add(projectile);
                            }

                            lastActionTimeStamp = action.Timestamp;

                            // action was added after current tick has been started - leave loop
                            if (now - action.Timestamp < 0)
                            {
                                break;
                            }
                        }

                        player.LastMovementDirection = movementDirection;

                        // check if player got hit
                        foreach (var proj in game.Projectiles)
                        {
                            var dx = proj.X - player.X;
                            var dy = proj.Y - player.Y;

                            if (Math.Sqrt(dx * dx + dy * dy) < _playerHitboxRadius + _projectileHitboxRadius)
                            {
                                // TODO handle death of player
                                break;
                            }
                        }
                    }
                    // TODO send to every player in game updated state
                    await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("serverTick", new GameState()
                    {
                        DeletedProjectiles = deletedProjectiles,
                        NewProjectiles = newProjectiles,
                        Players = game.Players.Select(p => new PlayerState()
                        {
                            Id = p.Id,
                            R = p.R,
                            X = p.X,
                            Y = p.Y
                        }),
                        TimeStamp = now
                    });
                }
                // update timestamp of last tick
                _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }
    }
}
