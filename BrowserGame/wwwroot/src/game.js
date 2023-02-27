"use strict";

import { Loader } from "./game/loader.js";
import { Painter } from "./game/painter.js";
import { Connection } from "./game/connection.js";
import { vectorAngle, movementDirection } from "./game/utils.js";
import { Menu } from './game/menu.js';
import { Scoreboard } from "./game/scoreboard.js";
import { Message } from "./game/message.js";

const params = new URLSearchParams(window.location.search);

// check validity of parameters for creating or joining game
if(!((params.get("action") === "join" && params.get("playerName") && params.get("gameId") && params.get("map")) || 
(params.get("action") === "create" && params.get("playerName") && params.get("gameName") && params.get("map")))){
    // parameters are invalid, redirect to main menu
    document.location.href = "/index.html";
}

// array containing players participating in game
const players = [];

// array of projectiles on a map
const projectiles = [];

// array of messages shown
const messages = [];
messages.push(new Message('Press ` to view scoreboard.', 10000));
messages.push(new Message('Press ESC to open menu', 10000));

// connection with signalr server
const con = new Connection("gamehub")
    .setPlayers(players)
    .setProjectiles(projectiles)
    .setMessages(messages);

// starting connection
await con.start();

// joining game
if(params.get("action") == "join"){
    await con.joinGame(params.get("gameId"), params.get("playerName"));
}

//creating game
if(params.get("action") == "create"){
    await con.createGame(params.get("playerName"), params.get("gameName"), params.get("map"));
}

const client = players.find(p => p.id == con.connectionId);
client.lastUpdate = performance.now();

const app = new PIXI.Application({
    width: 840,
    height: 640,
    transparent: false,
    antialias: true
});

const loader = new Loader();
const map = await loader.loadMap(params.get("map"));
const playerTexture = await loader.loadTexture("playerDefault");
const projectileTexture = await loader.loadTexture("projectileDefault");

const painter = new Painter(app)
    .setMap(map)
    .centerCameraOn(client)
    .setDefaultPlayerTexture(playerTexture)
    .setDefaultProjectileTexture(projectileTexture)
    .setPlayers(players)
    .setProjectiles(projectiles)
    .setMessages(messages)
    .draw();

const menu = new Menu()
    .setPosition(painter.width() * 0.25, painter.height() * 0.25)
    .setDimensions(painter.width() * 0.5, painter.height() * 0.65)
    .addButton('Exit to menu', () => {
        document.location.href = "/index.html";
    })
    .addToStage(painter.app.stage);

const scoreboard = new Scoreboard()
    .setPlayers(players)
    .setPosition(painter.width() * 0.2, painter.height() * 0.25)
    .setDimensions(painter.width() * 0.6, painter.height() * 0.65);

let movX = 0;
let movY = 0;

window.addEventListener("keyup", e => {
    switch(e.key){
        case "`":
            scoreboard.deleteFromStage(painter.app.stage);
            break;
    }
});

window.addEventListener("keydown", e => {
    switch(e.key){
        case "`":
            if(menu.visible === false){
                scoreboard.addToStage(painter.app.stage);
            }
            break;
        case "Escape":
            menu.toggle();
            break;
        case "Down": // IE/Edge specific value
        case "ArrowDown":
            if(movY !== 1){
                con.updateState({movementDirection: movementDirection(movX, 1)});
            }
            movY = 1;
            break;
        case "Up": // IE/Edge specific value
        case "ArrowUp":
            if(movY !== -1){
                con.updateState({movementDirection: movementDirection(movX, -1)});
            }
            movY = -1;
        break;
        case "Left": // IE/Edge specific value
        case "ArrowLeft":
            if(movX !== -1){
                con.updateState({movementDirection: movementDirection(-1, movY)});
            }
            movX = -1;
        break;
        case "Right": // IE/Edge specific value
        case "ArrowRight":
            if(movX !== 1){
                con.updateState({movementDirection: movementDirection(1, movY)});
            }
            movX = 1;
        break;
    }
});

window.addEventListener("keyup", e => {
    switch(e.key){
        case "Down": // IE/Edge specific value
        case "ArrowDown":
        case "Up": // IE/Edge specific value
        case "ArrowUp":
            if(movY !== 0){
                con.updateState({movementDirection: movementDirection(movX, 0)});
            }
            movY = 0;
            break;
        case "Left": // IE/Edge specific value
        case "ArrowLeft":
        case "Right": // IE/Edge specific value
        case "ArrowRight":
            if(movX !== 0){
                con.updateState({movementDirection: movementDirection(0, movY)});
            }
            movX = 0;
            break;
    }
});

app.stage.interactive = true;

app.stage.on("pointermove", e => {
    client.rotation = vectorAngle(e.data.global.x - app.screen.width * 0.5, e.data.global.y - app.screen.height * 0.5);
});

app.stage.on("pointerdown", e => {
    con.updateState({movementDirection: movementDirection(movX, movY), action: 0});
});

app.ticker.add(d => {
    const delta = app.ticker.elapsedMS;

    con.updateState({movementDirection: movementDirection(movX, movY)});

    for(let i = projectiles.length - 1; i >= 0; --i){
        const projectile = projectiles[i];

        projectile.x += projectile.velocityX * delta;
        projectile.y += projectile.velocityY * delta;

        // projectile's outside of map boundries - delete it
        if(projectile.x < 0 || projectile.y < 0 || projectile.x > map.width * map.tileWidth || projectile.y > map.height * map.tileHeight){
            projectiles.splice(i, 1);
            continue;
        }

        // projectile hit an obstacle - delete it
        if(!map.shootThroughMap[Math.floor(projectile.y / map.tileHeight)][Math.floor(projectile.x / map.tileWidth)]){
            projectiles.splice(i, 1);
        }

        for(const player of players){
            // player can't be hit by his own projectile
            if(projectile.playerId === player.id){
                continue;
            }

            // distance between center of projectile and center of player
            const distance = Math.sqrt(Math.pow(player.x - projectile.x, 2) + Math.pow(player.y - projectile.y, 2));

            // player got hit - delete projectile
            if(distance < map.playerHitboxRadius + map.projectileHitboxRadius){
                projectiles.splice(i, 1);
                break;
            }
        }
    }

    for(let i = messages.length - 1; i >= 0; --i){
        messages[i].ttl -= delta;
        
        if(messages[i].ttl <= 0){
            messages.splice(i, 1);
        }
    }

    painter.update();
});
