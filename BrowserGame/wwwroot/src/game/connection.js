import { Player } from "./player.js";
import { Projectile } from "./projectile.js";
import { Message } from "./message.js";

export class Connection{
    constructor(url){
        this.con = new signalR.HubConnectionBuilder()
            .withUrl(`/${url}`)
            /*.withHubProtocol(new MessagePackHubProtocol)*/
            .configureLogging(signalR.LogLevel.Information)
            .build();

        this.con.onclose(async () => {
            await this.start();
        })

        this.lastUpdate = performance.now();
        return this;
    }

    // connects with server
    // after connection has been established runs game function 
    async start(){
        await this.con.start();
        console.log("SignalR connected");
        this.connectionId = this.con.connectionId;
    }

    handleTick = (state) => {
        state.players.forEach(p => {
            const player = this.players.find(e => e.id === p.id);
            if(player !== undefined){
                player.x = p.x;
                player.y = p.y;
                player.rotation = p.r;
            }
        });

        state.newProjectiles.forEach(p => {
            const proj = new Projectile({
                id: p.id,
                pId: p.pId,
                sx: p.sx,
                sy: p.sy,
                x: p.x,
                y: p.y
            });
            this.projectiles.push(proj);
        });
    }

    handlePlayerJoin = (player) => {
        this.messages.push(new Message(`${player.name} has joined`));
        this.players.push(new Player(player));
    }

    handlePlayerLeave = (playerId) => {
        this.messages.push(new Message(`${this.players.find(p => p.id === playerId).name} has left`))
        this.players.splice(this.players.findIndex(p => p.id === playerId), 1)
    }

    handlePlayerKilled = (playerId, killerId) => {
        const player = this.players.find(p => p.id === playerId);
        const killer = this.players.find(p => p.id === killerId);

        if(player !== undefined){
            player.deaths++;
        }

        if(killer !== undefined){
            killer.kills++;
        }

        if(playerId === this.client.id){
            this.messages.push(new Message(`You have been killed by ${killer === undefined ? "unknown" : killer.name}`));
            return;
        }

        if(killerId === this.client.id){
            this.messages.push(new Message(`You have killed ${player === undefined ? "unknown" : player.name}`));
            return;
        }

        this.messages.push(new Message(`${killer === undefined ? "unknown" : killer.name} has killed ${player === undefined ? "unknown" : player.name}`));
    }

    async joinGame(gameId, playerName){
        return new Promise((resolve, reject) => {
            this.con.on("GameJoined", state => {    
                state.players.forEach(p => {
                    this.players.push(new Player(p));
                });
                    
                state.projectiles.forEach(p => {
                    this.projectiles.push(new Projectile(p));
                });

                this.client = this.players.find(p => p.id === this.con.connectionId);

                this.con.on("ServerTick", this.handleTick);
                this.con.on("PlayerJoined", this.handlePlayerJoin);
                this.con.on("PlayerLeft", this.handlePlayerLeave);
                this.con.on("PlayerKilled", this.handlePlayerKilled);

                resolve();
            });

            this.con.on("JoinError", () => {
                document.getElementById("gameContainer").innerHTML = "<p>Could not join game.</p>";
                setTimeout(() => {
                    document.location.href = "/join.html";
                }, 2000);

                reject("Could not join");
            });
            
            this.con.invoke("JoinRoom", gameId, playerName)
            .catch(err => reject(err));
        })
    }

    async createGame(playerName, gameName, map){
        return new Promise((resolve, reject) => {
            this.con.on("GameJoined", state => {    
                state.players.forEach(p => {
                    this.players.push(new Player(p));
                });
                    
                state.projectiles.forEach(p => {
                    this.projectiles.push(new Projectile(p));
                });

                this.client = this.players.find(p => p.id === this.con.connectionId);

                this.con.on("ServerTick", this.handleTick);
                this.con.on("PlayerJoined", this.handlePlayerJoin);
                this.con.on("PlayerLeft", this.handlePlayerLeave);
                this.con.on("PlayerKilled", this.handlePlayerKilled);

                resolve();
            });

            this.con.on("CreateError", () => {
                document.getElementById("gameContainer").innerHTML = "<p>Could not create game.</p>";
                setTimeout(() => {
                    document.location.href = "/create.html";
                }, 2000);

                reject();
            });
            
            this.con.invoke("CreateRoom", {playerName: playerName, gameName: gameName, map: map})
            .catch(err => reject());
        })
    }

    // sends updated client's state to the server
    // movementDirection is an enum containing information about the direction of player's movement, where
    // 0 - upper-left, 1 - up, 2 - upper-right
    // 3 - left, 4 - no movement, 5 - right
    // 4 - bottom-left, 5 - down, 6 - bottom-right
    // action is an enum containing information about other player's actions, where
    // 0 - shot
    async updateState(state){
        if(performance.now - this.lastUpdate < 10 && state.movementDirection == undefined && state.action == undefined){
            return;
        }
        
        this.lastUpdate = performance.now;
        await this.con.invoke("UpdateState", state.movementDirection, this.client.rotation, state.action);
    }

    setPlayers(playerArr){
        this.players = playerArr;
        return this;
    }

    setProjectiles(projectilesArr){
        this.projectiles = projectilesArr;
        return this;
    }

    setMessages(messageArr){
        this.messages = messageArr;
        return this;
    }
};
