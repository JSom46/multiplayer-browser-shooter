using BrowserGame.Data;
using BrowserGame.Dtos;
using BrowserGame.Hubs;
using BrowserGame.Models;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGame
{
    public class GameUpdateService : BackgroundService
    {
        private readonly IGameData _gameData;
        private readonly IMapData _mapData;
        private readonly IHubContext<GameHub> _hubContext;
        private readonly PeriodicTimer _timer;
        private long _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private const int Tick = 17;
        private const double Epsilon = 0.00001;

        public GameUpdateService(IGameData gameData, IMapData mapData, IHubContext<GameHub> hubContext)
        {
            _gameData = gameData;
            _mapData = mapData;
            _hubContext = hubContext;
            _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Tick));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (await _timer.WaitForNextTickAsync(stoppingToken))
            {
                // update state of each game
                foreach (var game in _gameData.GetAll())
                {
                    var map = _mapData.GetByName(game.MapName);
                    var playerHitboxRadius = 0.4 * Math.Max(map.TileHeight, map.TileWidth);
                    var projectileHitboxRadius = 0.25 * Math.Max(map.TileHeight, map.TileWidth);
                    var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var deltaTime = now - _lastTick;
                    var newProjectiles = new List<ProjectileModel>();

                    // update state of each projectile in game
                    for (var i = game.Projectiles.Count - 1; i >= 0; --i)
                    {
                        game.Projectiles[i].X += game.Projectiles[i].SX * deltaTime;
                        game.Projectiles[i].Y += game.Projectiles[i].SY * deltaTime;

                        // projectile is inside map's boundries and didn't hit any obstacle
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
                            MovePlayer(player, map, action.Timestamp - player.LastStateUpdate, playerHitboxRadius);

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
                        MovePlayer(player, map, now - player.LastStateUpdate, playerHitboxRadius);

                        // check if player got hit by other player's projectile
                        foreach (var projectile in game.Projectiles)
                        {
                            // player's own projectile can't hit him
                            if (player.Id == projectile.PId)
                            {
                                continue;
                            }

                            // distance between centers of player's model and projectile
                            var distance = Math.Sqrt(Math.Pow(player.X - projectile.X, 2) + Math.Pow(player.Y - projectile.Y, 2));

                            // player wasn't hit by a projectile
                            if (distance >= playerHitboxRadius + projectileHitboxRadius)
                            {
                                continue;
                            }

                            // get player's killer
                            var killer = game.Players.FirstOrDefault(p => p.Id == projectile.PId);

                            // handle player's death
                            player.Deaths++;
                            RandomPlayerPosition(player, map);

                            if (killer != null)
                            {
                                killer.Kills++;
                            }

                            game.Projectiles.Remove(projectile);

                            await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("playerKilled", player.Id, killer?.Id);

                            break;
                        }

                        // update player's last update timestamp
                        player.LastStateUpdate = now;
                    }

                    await _hubContext.Clients.Group(game.Id.ToString()).SendAsync("serverTick", new GameState()
                    {
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

        protected void MovePlayer(PlayerModel player, MapModel map, long delta, double playerHitboxRadius)
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
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                        var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                        var x = (int)((newX - playerHitboxRadius) / map.TileWidth);

                        // player can't move left
                        if (y1 < 0 || y2 >= map.Height || x < 0 || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + playerHitboxRadius;
                        }

                        var y = (int)((newY - playerHitboxRadius) / map.TileHeight);
                        var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                        var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                        // player can't move up
                        if (y < 0 || x1 < 0 || x2 >= map.Width || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + playerHitboxRadius;
                        }

                        break;
                    }
                    // up
                    case 1:
                    {
                        newY += -player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y = (int)((newY - playerHitboxRadius) / map.TileHeight);
                        var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                        var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                        // player can't move up
                        if (y < 0 || x1 < 0 || x2 >= map.Width || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + playerHitboxRadius;
                        }

                        break;
                    }
                    // upper - right
                    case 2:
                    {
                        newX += player.MovementSpeed * delta;
                        newY += -player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                        var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                        var x = (int)((newX + playerHitboxRadius) / map.TileWidth);

                        // player can't move right
                        if (x >= map.Width || y1 < 0 || y2 >= map.Height || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - playerHitboxRadius;
                        }

                        var y = (int)((newY - playerHitboxRadius) / map.TileHeight);
                        var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                        var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                        // player can't move up
                        if (y < 0 || x1 < 0 || x2 >= map.Width || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                        {
                            newY = Math.Floor(player.Y / map.TileHeight) * map.TileHeight + playerHitboxRadius;
                        }

                        break;
                    }
                    // left
                    case 3:
                    {
                        newX += -player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                        var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                        var x = (int)((newX - playerHitboxRadius) / map.TileWidth);

                        // player can't move left
                        if (y1 < 0 || y2 >= map.Height || x <= 0 || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + playerHitboxRadius;
                        }

                        break;
                    }
                    // right
                    case 5:
                    {
                        newX += player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                        var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                        var x = (int)((newX + playerHitboxRadius) / map.TileWidth);

                        // player can't move right
                        if (x >= map.Width || y1 < 0 || y2 >= map.Height || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - playerHitboxRadius;
                        }

                        break;
                    }
                    // bottom - left
                    case 6:
                    {
                        newX += -player.MovementSpeed * delta;
                        newY += player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }


                        var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                        var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                        var x = (int)((newX - playerHitboxRadius) / map.TileWidth);

                        // player can't move left
                        if (y1 < 0 || y2 >= map.Height || x < 0 || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                        {
                            newX = Math.Floor(player.X / map.TileWidth) * map.TileWidth + playerHitboxRadius;
                        }

                        var y = (int)((newY + playerHitboxRadius) / map.TileHeight);
                        var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                        var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                        // player can't move down
                        if (x1 < 0 || x2 >= map.Width || y >= map.Height || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - playerHitboxRadius;
                        }

                        break;
                    }
                    // bottom
                    case 7:
                    {
                        newY += player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y = (int)((newY + playerHitboxRadius) / map.TileHeight);
                        var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                        var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                        // player can't move down
                        if (x1 < 0 || x2 >= map.Width || y >= map.Height || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - playerHitboxRadius;
                        }

                        break;
                    }
                    // bottom - right
                    case 8:
                    {
                        newX += player.MovementSpeed * delta;
                        newY += player.MovementSpeed * delta;

                        // player out of map's boundries
                        if (newX - playerHitboxRadius <= 0)
                        {
                            newX = playerHitboxRadius;
                        }
                        if (newY - playerHitboxRadius <= 0)
                        {
                            newY = playerHitboxRadius;
                        }
                        if (newX + playerHitboxRadius >= map.Width * map.TileWidth)
                        {
                            newX = map.Width * map.TileWidth - playerHitboxRadius;
                        }
                        if (newY + playerHitboxRadius >= map.Height * map.TileHeight)
                        {
                            newY = map.Height * map.TileHeight - playerHitboxRadius;
                        }

                        var y1 = (int)((player.Y - playerHitboxRadius + Epsilon) / map.TileHeight);
                        var y2 = (int)((player.Y + playerHitboxRadius - Epsilon) / map.TileHeight);
                        var x = (int)((newX + playerHitboxRadius) / map.TileWidth);

                        // player can't move right
                        if (x >= map.Width || y1 < 0 || y2 >= map.Height || !map.IsTraversable[y1][x] || !map.IsTraversable[y2][x])
                        {
                            newX = Math.Ceiling(player.X / map.TileWidth) * map.TileWidth - playerHitboxRadius;
                        }

                        var y = (int)((newY + playerHitboxRadius) / map.TileHeight);
                        var x1 = (int)((newX - playerHitboxRadius + Epsilon) / map.TileWidth);
                        var x2 = (int)((newX + playerHitboxRadius - Epsilon) / map.TileWidth);

                        // player can't move down
                        if (x1 < 0 || x2 >= map.Width || y >= map.Height || !map.IsTraversable[y][x1] || !map.IsTraversable[y][x2])
                        {
                            newY = Math.Ceiling(player.Y / map.TileHeight) * map.TileHeight - playerHitboxRadius;
                        }

                        break;
                    }
                }
            }
            catch (IndexOutOfRangeException e)
            {
                Console.WriteLine("x: " + player.X + " y: " + player.Y + " direction: " + player.LastMovementDirection + " newx: " +
                                  newX + " newy: " + newY + " y1: " + ((int)(player.Y - playerHitboxRadius) / map.TileHeight) +
                                  " y2: " + (int)((player.Y + playerHitboxRadius) / map.TileHeight) + " x1: " +
                                  (int)((newX - playerHitboxRadius) / map.TileWidth) + " x2: " +
                                  (int)((newX + playerHitboxRadius) / map.TileWidth) + " x + e: " +
                                  (newX + playerHitboxRadius) + " x - e: " + (newX - playerHitboxRadius) + " y + e: " +
                                  (newY + playerHitboxRadius) + " y - e: " + (newY - playerHitboxRadius) + "mapwidth: " + 
                                  map.IsTraversable[0].Length + "mapheight" + map.IsTraversable.Length);
                Console.WriteLine(e.StackTrace);
                throw e;
            }

            player.X = newX;
            player.Y = newY;
        }

        /// <summary>
        /// change player's position to a center of random, traversable field
        /// </summary>
        /// <param name="player">player whose position is to be changed</param>
        /// <param name="map">map the player is moving on</param>
        protected void RandomPlayerPosition(PlayerModel player, MapModel map)
        {
            var rng = new Random();
            var x = rng.Next(0, map.Width);
            var y = rng.Next(0, map.Height);

            while (!map.IsTraversable[y][x])
            {
                x = rng.Next(0, map.Width);
                y = rng.Next(0, map.Height);
            }

            player.X = x * map.TileWidth + map.TileWidth * 0.5;
            player.Y = y * map.TileHeight + map.TileHeight * 0.5;
        }
    }
}
