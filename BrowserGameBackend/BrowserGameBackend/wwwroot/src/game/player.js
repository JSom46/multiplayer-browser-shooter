export class Player{
    constructor(data){
        this.id = data.id;
        this.x = data.x;
        this.y = data.y;
        this.rotation = data.r;
        this.movementSpeed = data.movementSpeed;
        this.name = data.name;
        this.kills = data.kills;
        this.deaths = data.deaths;
        this.projectilesSpeed = data.projectilesSpeed;
    }
};
