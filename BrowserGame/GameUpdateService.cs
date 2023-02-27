using BrowserGame.Data;
using BrowserGame.Dtos;
using BrowserGame.Hubs;
using BrowserGame.Models;
using BrowserGame.Utils;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGame;

public class GameUpdateService : BackgroundService
{
    private const int Tick = 17;
    private readonly IGameData _gameData;
    private readonly IHubContext<GameHub, IGameClient> _hubContext;
    private readonly IMapData _mapData;
    private readonly PeriodicTimer _timer;
    private readonly IPlayerPositioner _playerPositioner;
    private long _lastTick = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public GameUpdateService(IGameData gameData, IMapData mapData, IHubContext<GameHub, IGameClient> hubContext, IPlayerPositioner playerPositioner)
    {
        _gameData = gameData;
        _mapData = mapData;
        _hubContext = hubContext;
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(Tick));
        _playerPositioner = playerPositioner;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (await _timer.WaitForNextTickAsync(cancellationToken))
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
                        map.CanBeShotThrough[(int)(game.Projectiles[i].Y / map.TileHeight)][
                            (int)(game.Projectiles[i].X / map.TileWidth)])
                        continue;

                    // projectile is outside of map's boundries - delete it
                    game.Projectiles.RemoveAt(i);
                }

                // update state of each player in game
                foreach (var player in game.Players)
                {
                    // process all enqueued actions 
                    while (player.Moves.TryDequeue(out var action))
                    {
                        // process movement
                        _playerPositioner.MovePlayer(player, map, action.Timestamp - player.LastStateUpdate,
                            playerHitboxRadius);

                        // change players movement direction
                        player.LastMovementDirection = action.MovementDirection;

                        // change players rotation
                        player.R = action.R;

                        // execute action if it's been defined in action (I'm bad at naming stuff)
                        if (action.Action != null)
                            switch (action.Action)
                            {
                                // shoot
                                case 0:
                                {
                                    var proj = new ProjectileModel
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

                        // update player's last update timestamp
                        player.LastStateUpdate = action.Timestamp;
                    }

                    // process movement between last action timestamp and now
                    _playerPositioner.MovePlayer(player, map, now - player.LastStateUpdate, playerHitboxRadius);

                    // check if player got hit by other player's projectile
                    foreach (var projectile in game.Projectiles)
                    {
                        // player's own projectile can't hit him
                        if (player.Id == projectile.PId) continue;

                        // distance between centers of player's model and projectile
                        var distance = Math.Sqrt(Math.Pow(player.X - projectile.X, 2) +
                                                 Math.Pow(player.Y - projectile.Y, 2));

                        // player wasn't hit by a projectile
                        if (distance >= playerHitboxRadius + projectileHitboxRadius) continue;

                        // get player's killer
                        var killer = game.Players.FirstOrDefault(p => p.Id == projectile.PId);

                        // handle player's death
                        player.Deaths++;
                        _playerPositioner.RandomPlayerPosition(player, map);

                        if (killer != null) killer.Kills++;

                        game.Projectiles.Remove(projectile);

                        await _hubContext.Clients.Group(game.Id.ToString())
                            .PlayerKilled(player.Id, killer?.Id);

                        break;
                    }

                    // update player's last update timestamp
                    player.LastStateUpdate = now;
                }

                await _hubContext.Clients.Group(game.Id.ToString()).ServerTick(new GameState
                {
                    NewProjectiles = newProjectiles,
                    Players = game.Players.Select(p => new PlayerState
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