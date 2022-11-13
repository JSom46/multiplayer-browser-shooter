"use strict";

import { Loader } from "./game/loader.js";
import { Painter } from "./game/painter.js";
import { Player } from "./game/player.js";
import { Projectile } from "./game/projectile.js";
import { Connection } from "./game/connection.js";
import { vectorAngle } from "./game/muchMath.js";

const params = new URLSearchParams(window.location.search);

// check validity of parameters for creating or joining game
if(!((params.get("action") === "join" && params.get("playerName") && params.get("gameId") && params.get("map")) || 
(params.get("action") === "create" && params.get("playerName") && params.get("gameName") && params.get("map")))){
    // parameters are invalid, redirect to main menu
    document.location.href = "/index.html";
}

const players = [];
const projectiles = [];

const con = new Connection("gamehub");
con.setPlayers(players);
con.setProjectiles(projectiles)

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

let movX = 0;
let movY = 0;

const movementDirection = (x, y) => {
    if(x == -1 && y == -1) return 0;
    if(x == 0 && y == -1) return 1;
    if(x == 1 && y == -1) return 2;
    if(x == -1 && y == 0) return 3;
    if(x == 0 && y == 0) return 4;
    if(x == 1 && y == 0) return 5;
    if(x == -1 && y == 1) return 6;
    if(x == 0 && y == 1) return 7;
    if(x == 1 && y == 1) return 8;
}

window.addEventListener("keydown", e => {
    switch(e.key){
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

const painter = new Painter(app);
painter.setMap(map);
painter.centerCameraOn(client);
painter.setDefaultPlayerTexture(playerTexture);
painter.setDefaultProjectileTexture(projectileTexture);
painter.setPlayers(players);
painter.setProjectiles(projectiles);
painter.draw();

app.stage.interactive = true;

app.stage.on("pointermove", e => {
    client.rotation = vectorAngle(e.data.global.x - app.screen.width * 0.5, e.data.global.y - app.screen.height * 0.5);
});

app.stage.on("pointerdown", e => {
    con.updateState({movementDirection: movementDirection(movX, movY), action: 0});
});

app.ticker.add(delta => {
    con.updateState({movementDirection: movementDirection(movX, movY)});

    for(let i = projectiles.length - 1; i >= 0; --i){
        const p = projectiles[i];

        p.x += p.velocityX * app.ticker.elapsedMS;
        p.y += p.velocityY * app.ticker.elapsedMS;

        // projectile's outside of map boundries - delete it
        if(p.x < 0 || p.y < 0 || p.x > map.width * map.tileWidth || p.y > map.height * map.tileHeight){
            console.log(`out of bounds: y: ${Math.floor(p.y / map.tileHeight)} x: ${Math.floor(p.x / map.tileWidth)}`);
            projectiles.splice(i, 1);
            continue;
        }

        // projectile hit an obstacle - delete it
        if(!map.shootThroughMap[Math.floor(p.y / map.tileHeight)][Math.floor(p.x / map.tileWidth)]){
            console.log(`obstacle: y: ${Math.floor(p.y / map.tileHeight)} x: ${Math.floor(p.x / map.tileWidth)}`);
            projectiles.splice(i, 1);
        }
    }





    painter.update();
});
