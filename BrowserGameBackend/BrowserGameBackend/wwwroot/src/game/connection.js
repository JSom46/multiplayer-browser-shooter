import { Player } from "./player.js";
import { Projectile } from "./projectile.js";

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
    }

    // connects with server
    // after connection has been established runs game function 
    async start(){
        await this.con.start();
        console.log("SignalR connected");
        this.connectionId = this.con.connectionId;
    }

    async joinGame(gameId, playerName){
        return new Promise((resolve, reject) => {
            this.con.on("gameJoined", state => {    
                state.players.forEach(p => {
                    this.players.push(new Player(p));
                });
                    
                state.projectiles.forEach(p => {
                    this.projectiles.push(new Projectile(p));
                });

                this.con.on("serverTick", (state) => {
                    //console.log(state);
                    if(state.deletedProjectiles.length > 0){
                        console.log(state);
                    }
                })

                resolve();
            });

            this.con.on("joinError", () => {
                document.getElementById("gameContainer").innerHTML = "<p>Could not join game.</p>";
                setTimeout(() => {
                    document.location.href = "/join.html";
                }, 2000);

                reject();
            });
            
            this.con.invoke("JoinRoom", gameId, playerName)
            .catch(err => reject());
        })
    }

    async createGame(playerName, gameName, map){
        return new Promise((resolve, reject) => {
            this.con.on("gameJoined", state => {    
                state.players.forEach(p => {
                    this.players.push(new Player(p));
                });
                    
                state.projectiles.forEach(p => {
                    this.projectiles.push(new Projectile(p));
                });

                this.con.on("serverTick", (state) => {
                    //console.log(state);
                    if(state.deletedProjectiles.length > 0){
                        console.log(state);
                    }
                })

                resolve();
            });

            this.con.on("createError", () => {
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
    // rotation is player's rotation in radians
    // action is an enum containing information about other player's actions, where
    // 0 - shot
    async updateState(state){
        if(state.movementDirection == undefined && state.rotation == undefined && state.action == undefined){
            return;
        }

        if(performance.now - this.lastUpdate < 5 && state.movementDirection == undefined && state.action == undefined){
            return;
        }
        
        await this.con.invoke("UpdateState", state.movementDirection, state.rotation, state.action);
    }

    setPlayers(playerArr){
        this.players = playerArr;
    }

    setProjectiles(projectilesArr){
        this.projectiles = projectilesArr;
    }
};
