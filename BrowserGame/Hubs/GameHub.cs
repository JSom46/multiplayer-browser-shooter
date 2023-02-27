﻿using BrowserGame.Data;
using BrowserGame.Dtos;
using BrowserGame.Models;
using BrowserGame.Utils;
using Microsoft.AspNetCore.SignalR;

namespace BrowserGame.Hubs;

public class GameHub : Hub<IGameClient>
{
    private readonly IGameData _games;
    private readonly IMapData _maps;
    private readonly IPlayerPositioner _playerPositioner;

    public GameHub(IMapData maps, IGameData games, IPlayerPositioner playerPositioner)
    {
        _maps = maps;
        _games = games;
        _playerPositioner = playerPositioner;
    }

    public override async Task<Task> OnDisconnectedAsync(Exception? exception)
    {
        // check if player was in any game
        var game = _games.GetByPlayerId(Context.ConnectionId);
        if (game == null) return base.OnDisconnectedAsync(exception);

        // delete player from the game
        _games.DeletePlayer(Context.ConnectionId);

        // if player was the only one in the game, delete game
        if (game.Players.Count == 0) _games.DeleteGame(game.Id);

        await Clients.Group(game.Id.ToString()).PlayerLeft(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }

    public async Task CreateRoom(CreateGameData data)
    {
        // check if specified map exists
        var map = _maps.GetByName(data.Map);
        if (map == null)
        {
            await Clients.Caller.CreateError();
            return;
        }

        // create game object and add it to games' list
        var gameId = Guid.NewGuid();
        var game = new GameModel
        {
            Id = gameId,
            MapName = data.Map,
            MaxPlayers = 32,
            Name = data.GameName
        };
        _games.AddGame(game);

        var player = new PlayerModel
        {
            Id = Context.ConnectionId,
            Name = data.PlayerName,
            X = 0,
            Y = 0,
            LastStateUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // set player's position
        _playerPositioner.RandomPlayerPosition(player, map);

        // add player to players' list
        _games.AddPlayer(player, gameId);

        // create game's group and add player to it
        await Groups.AddToGroupAsync(Context.ConnectionId, game.Id.ToString());

        // send to player response with game's state
        await Clients.Caller.GameJoined(new FullGameState
        {
            Players = game.Players.Select(p => new FullPlayerState
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
            await Clients.Caller.JoinError();
            return;
        }

        // check if player is not in another game
        if (_games.GetByPlayerId(Context.ConnectionId) != null)
        {
            await Clients.Caller.JoinError();
            return;
        }

        var game = _games.GetById(gameId);

        // check if game exists
        if (game == null)
        {
            await Clients.Caller.JoinError();
            return;
        }

        // check if there's a room for a player
        if (game.Players.Count >= game.MaxPlayers)
        {
            await Clients.Caller.JoinError();
            return;
        }

        // get map's model
        var map = _maps.GetByName(game.MapName);

        var player = new PlayerModel
        {
            Id = Context.ConnectionId,
            Name = playerName,
            X = 0,
            Y = 0,
            LastStateUpdate = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // set player's initial position
        _playerPositioner.RandomPlayerPosition(player, map);

        // add player to players' list
        _games.AddPlayer(player, gameId);

        // inform other players about new player
        await Clients.Group(gameId.ToString()).PlayerJoined(new FullPlayerState
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

        await Clients.Caller.GameJoined(new FullGameState
        {
            Players = game.Players.Select(p => new FullPlayerState
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
            await Clients.Caller.LeaveError();
            return;
        }

        var game = _games.GetById(gameId);

        // check if game exists
        if (game == null)
        {
            await Clients.Caller.LeaveError();
            return;
        }

        // remove player from game list and inform other players about leaving
        if (_games.DeletePlayer(Context.ConnectionId) == 0)
            await Clients.Group(gameIdString).PlayerLeft(Context.ConnectionId);

        // if room is empty, delete it
        if (game.Players.Count == 0) _games.DeleteGame(gameId);

        await Clients.Caller.GameLeft();
    }

    public void UpdateState(int movementDirection, double rotation, int? action)
    {
        var player = _games.GetPlayerById(Context.ConnectionId);

        if (player == null) return;

        try
        {
            player.Moves.Enqueue(new PlayerActionModel(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                movementDirection, rotation, action));
        }
        catch (ArgumentException e)
        {
            Console.WriteLine(e.Message);
            Console.WriteLine(e.StackTrace);
            Console.WriteLine(player.Moves.Count);
        }
    }
}