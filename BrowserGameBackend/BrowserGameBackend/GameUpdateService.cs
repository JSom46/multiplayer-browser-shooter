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
        private int _tick = 17;
        private readonly PeriodicTimer _timer;
        private long _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private readonly double _playerHitboxRadius = 0.4 * 32;
        private readonly double _projectileHitboxRadius = 0.25 * 32;
        private readonly double _epsilon = 0.00001;

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
                    Console.WriteLine("projNum: " + game.Projectiles.Count + " playNum: " + game.Players.Count);
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
                        // process all enqueued actions 
                        while (player.Moves.TryDequeue(out var action))
                        {
                            // process movement
                            HandleMovement(player, map, action.Timestamp - player.LastStateUpdate);

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

                            // update player's last update timestamp
                            player.LastStateUpdate = action.Timestamp;
                        }

                        // process movement between last action timestamp and now
                        HandleMovement(player, map, now - player.LastStateUpdate);

                        // update player's last update timestamp
                        player.LastStateUpdate = now;
                    }

                    // check if any player got hit by projectile
                    for (var i = game.Projectiles.Count - 1; i >= 0; --i)
                    {
                        
                    }

                    await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("serverTick", new GameState()
                    {
                        DeletedProjectiles = new List<int>(),
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

        protected void HandleMovement(PlayerModel player, MapModel map, long delta)
        {
            var newX = player.X;
            var newY = player.Y;

            try
            {
                switch (player.LastMovementDirection)
                {
                    // upper - left
                    case 0:
                    {
                        newX += -player.MovementSpeed * delta;
                        newY += -player.MovementSpeed * delta;

                        // player can't move left
                        if (newX - _playerHitboxRadius - _epsilon <= 0 ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + _playerHitboxRadius + _epsilon;
                        }

                        // player can't move up
                        if (newY - _playerHitboxRadius - _epsilon <= 0 ||
                            !map.IsTraversable[(int)Math.Truncate((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // up
                    case 1:
                    {
                        newY += -player.MovementSpeed * delta;

                        // player can't move up
                        if (newY - _playerHitboxRadius - _epsilon <= 0 ||
                            !map.IsTraversable[(int)Math.Truncate((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // upper - right
                    case 2:
                    {
                        newX += player.MovementSpeed * delta;
                        newY += -player.MovementSpeed * delta;

                        // player can't move right
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move up
                        if (newY - _playerHitboxRadius - _epsilon <= 0 ||
                            !map.IsTraversable[(int)Math.Truncate((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // left
                    case 3:
                    {
                        newX += -player.MovementSpeed * delta;

                        // player can't move left
                        if (newX - _playerHitboxRadius - _epsilon <= 0 ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // right
                    case 5:
                    {
                        newX += player.MovementSpeed * delta;

                        // player can't move right
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }

                        break;
                    }
                    // bottom - left
                    case 6:
                    {
                        newX += -player.MovementSpeed * delta;
                        newY += player.MovementSpeed * delta;

                        // player can't move left
                        if (newX - _playerHitboxRadius - _epsilon <= 0 ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + _playerHitboxRadius + _epsilon;
                        }

                        // player can't move down
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight || 
                            !map.IsTraversable[(int)Math.Truncate((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        break;
                    }
                    // bottom
                    case 7:
                    {
                        newY += player.MovementSpeed * delta;

                        // player can't move down
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight ||
                            !map.IsTraversable[(int)Math.Truncate((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        break;
                    }
                    // bottom - right
                    case 8:
                    {
                        newX += player.MovementSpeed * delta;
                        newY += player.MovementSpeed * delta;

                        // player can't move right
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move down
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight ||
                            !map.IsTraversable[(int)Math.Truncate((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)Math.Truncate((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        break;
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine("x: " + player.X + " y: " + player.Y + " direction: " + player.LastMovementDirection + " newx: " +
                                  newX + " newy: " + newY + " y1: " + (int)Math.Truncate((player.Y - _playerHitboxRadius) / map.TileHeight) +
                                  " y2: " + (int)Math.Truncate((player.Y + _playerHitboxRadius) / map.TileHeight) + " x1: " +
                                  (int)Math.Truncate((newX - _playerHitboxRadius) / map.TileWidth) + " x2: " +
                                  (int)Math.Truncate((newX + _playerHitboxRadius) / map.TileWidth) + " x + e: " +
                                  (newX + _playerHitboxRadius + _epsilon) + " x - e: " + (newX - _playerHitboxRadius - _epsilon) + " y + e: " +
                                  (newY + _playerHitboxRadius + _epsilon) + " y - e: " + (newY - _playerHitboxRadius - _epsilon));
                Console.WriteLine(e.StackTrace);
                throw e;
            }

            player.X = newX;
            player.Y = newY;
        }
    }
}
