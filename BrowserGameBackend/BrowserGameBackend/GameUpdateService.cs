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
        private readonly double _epsilon = 0;

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

                        // check if projectile is inside map's boundries and didn't hit any obstacle
                        if (game.Projectiles[i].X <= map.Width * map.TileWidth &&
                            game.Projectiles[i].Y <= map.Height * map.TileHeight && 
                            game.Projectiles[i].X >= 0 && game.Projectiles[i].Y >= 0 &&
                            map.CanBeShotThrough[(int)(game.Projectiles[i].Y / map.TileHeight)][(int)(game.Projectiles[i].X / map.TileWidth)])
                        {
                            continue;
                        }

                        // projectile is outside of map's boundries - delete it
                        //deletedProjectiles.Add(game.Projectiles[i].Id);
                        game.Projectiles.RemoveAt(i);
                    }
                    
                    // update state of each player in game
                    foreach (var player in game.Players)
                    {
                        // process all enqueued actions 
                        while (player.Moves.TryDequeue(out var action))
                        {
                            // process movement
                            HandlePlayerMovement(player, map, action.Timestamp - player.LastStateUpdate);

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
                        HandlePlayerMovement(player, map, now - player.LastStateUpdate);

                        // check if player got hit by other player's projectile
                        foreach (var projectile in game.Projectiles)
                        {
                            // player's own projectile can't hit him
                            if (player.Id == projectile.PId)
                            {
                                continue;
                            }

                            // distance between centers of player and projectile
                            var distance = Math.Sqrt(Math.Pow(player.X - projectile.X, 2) + Math.Pow(player.Y - projectile.Y, 2));

                            // player wasn't hit by a projectile
                            if (distance >= _playerHitboxRadius + _projectileHitboxRadius)
                            {
                                continue;
                            }

                            var killer = game.Players.FirstOrDefault(p => p.Id == projectile.PId);

                            player.Deaths++;
                            player.IsAlive = false;

                            if (killer != null)
                            {
                                killer.Kills++;
                            }

                            deletedProjectiles.Add(projectile.Id);
                            game.Projectiles.Remove(projectile);

                            await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("playerKilled", player.Id, killer?.Id);

                            break;
                        }

                        // update player's last update timestamp
                        player.LastStateUpdate = now;
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

        protected void HandlePlayerMovement(PlayerModel player, MapModel map, long delta)
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

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move left
                        if (!map.IsTraversable[(int)((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + _playerHitboxRadius + _epsilon;
                        }

                        // player can't move up
                        if (!map.IsTraversable[(int)((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // up
                    case 1:
                    {
                        newY += -player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move up
                        if (!map.IsTraversable[(int)((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
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

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move right
                        if (!map.IsTraversable[(int)((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move up
                        if (!map.IsTraversable[(int)((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((newY - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // left
                    case 3:
                    {
                        newX += -player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move left
                        if (!map.IsTraversable[(int)((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + _playerHitboxRadius + _epsilon;
                        }

                        break;
                    }
                    // right
                    case 5:
                    {
                        newX += player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move right
                        if (!map.IsTraversable[(int)((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
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

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move left
                        if (!map.IsTraversable[(int)((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + _playerHitboxRadius + _epsilon;
                        }

                        // player can't move down
                        if (!map.IsTraversable[(int)((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        break;
                    }
                    // bottom
                    case 7:
                    {
                        newY += player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move down
                        if (!map.IsTraversable[(int)((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
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

                        // player out of map's boundries
                        if (newX - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newX = _playerHitboxRadius + _epsilon;
                        }
                        if (newY - _playerHitboxRadius - _epsilon <= 0)
                        {
                            newY = _playerHitboxRadius + _epsilon;
                        }
                        if (newX + _playerHitboxRadius + _epsilon >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }
                        if (newY + _playerHitboxRadius + _epsilon >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move right
                        if (!map.IsTraversable[(int)((player.Y - _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((player.Y + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - _playerHitboxRadius - _epsilon;
                        }

                        // player can't move down
                        if (!map.IsTraversable[(int)((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX - _playerHitboxRadius) / map.TileWidth)] ||
                            !map.IsTraversable[(int)((newY + _playerHitboxRadius) / map.TileHeight)]
                                [(int)((newX + _playerHitboxRadius) / map.TileWidth)])
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
