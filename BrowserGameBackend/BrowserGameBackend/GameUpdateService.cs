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
        private int _tick = 33;
        private readonly PeriodicTimer _timer;
        private long _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private readonly double _playerHitboxRadius = 0.4;
        private readonly double _projectileHitboxRadius = 0.25;

        public GameUpdateService(IGameData gameData, IMapData mapData, IHubContext<GameHub> hubContext)
        {
            _gameData = gameData;
            _mapData = mapData;
            _hubContext = hubContext;
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(_tick));
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

                        // check if projectile is inside map's boundries
                        if (game.Projectiles[i].X <= map.Width * map.TileWidth &&
                            game.Projectiles[i].Y <= map.Height * map.TileHeight && 
                            game.Projectiles[i].X >= 0 && game.Projectiles[i].Y >= 0 &&
                            map.CanBeShotThrough[(int)(game.Projectiles[i].Y / map.TileHeight)][(int)(game.Projectiles[i].X / map.TileWidth)])
                        {
                            continue;
                        }

                        // projectile is outside of map's boundries - delete it
                        deletedProjectiles.Add(game.Projectiles[i].Id);
                        game.Projectiles.Remove(game.Projectiles[i]);
                    }
                    
                    // update state of each player in game
                    foreach (var player in game.Players)
                    {
                        var dx = 0d;
                        var dy = 0d;
                        long timeSinceLastAction;
                        // process all enqueued actions 
                        while (player.Moves.TryDequeue(out var action))
                        {
                            // time between previously and currently processed action
                            timeSinceLastAction = action.Timestamp - player.LastStateUpdate;

                            // process movement
                            switch (player.LastMovementDirection)
                            {
                                // upper - left
                                case 0:
                                {
                                    dx = -player.MovementSpeed * timeSinceLastAction;
                                    dy = -player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // up
                                case 1:
                                {
                                    dy = -player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // upper - right
                                case 2:
                                {
                                    dx = player.MovementSpeed * timeSinceLastAction;
                                    dy = -player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // left
                                case 3:
                                {
                                    dx = -player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // right
                                case 5:
                                {
                                    dx = player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // bottom - left
                                case 6:
                                {
                                    dx = -player.MovementSpeed * timeSinceLastAction;
                                    dy = player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // bottom
                                case 7:
                                {
                                    dy = player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                                // bottom - right
                                case 8:
                                {
                                    dx = player.MovementSpeed * timeSinceLastAction;
                                    dy = player.MovementSpeed * timeSinceLastAction;
                                    break;
                                }
                            }

                            // change players movement direction
                            player.LastMovementDirection = action.MovementDirection;

                            // change players rotation
                            player.R = action.R;

                                // execute action if it's been defined in action (I'm bad at naming stuff)
                            if (action.Action != null)
                            {
                                switch (action.Action)
                                {
                                    // shoot
                                    case 0:
                                    {
                                        var proj = new ProjectileModel()
                                        {
                                            Id = game.NextProjectileId++,
                                            PId = player.Id,
                                            X = player.X,
                                            Y = player.Y,
                                            SX = player.ProjectilesSpeed * Math.Sin(player.R + Math.PI * 0.5),
                                            SY = player.ProjectilesSpeed * -Math.Cos(player.R + Math.PI * 0.5)
                                        };

                                        newProjectiles.Add(proj);
                                        game.Projectiles.Add(proj);

                                        break;
                                    }
                                }
                            }

                            // update player's position
                            player.X += dx;
                            player.Y += dy;

                            dx = 0d;
                            dy = 0d;

                            // update player's last update timestamp
                            player.LastStateUpdate = action.Timestamp;
                        }

                        // process movement between last action timestamp and now
                        timeSinceLastAction = now - player.LastStateUpdate;
                        dx = 0d;
                        dy = 0d;

                        switch (player.LastMovementDirection)
                        {
                            // upper - left
                            case 0:
                            {
                                dx = -player.MovementSpeed * timeSinceLastAction;
                                dy = -player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // up
                            case 1:
                            {
                                dy = -player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // upper - right
                            case 2:
                            {
                                dx = player.MovementSpeed * timeSinceLastAction;
                                dy = -player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // left
                            case 3:
                            {
                                dx = -player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // right
                            case 5:
                            {
                                dx = player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // bottom - left
                            case 6:
                            {
                                dx = -player.MovementSpeed * timeSinceLastAction;
                                dy = player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // bottom
                            case 7:
                            {
                                dy = player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                            // bottom - right
                            case 8:
                            {
                                dx = player.MovementSpeed * timeSinceLastAction;
                                dy = player.MovementSpeed * timeSinceLastAction;
                                break;
                            }
                        }

                        // update player's position
                        player.X += dx;
                        player.Y += dy;

                        // update player's last update timestamp
                        player.LastStateUpdate = now;
                    }

                    // check if any player got hit by projectile
                    for (var i = game.Projectiles.Count - 1; i >= 0; --i)
                    {
                        
                    }

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
