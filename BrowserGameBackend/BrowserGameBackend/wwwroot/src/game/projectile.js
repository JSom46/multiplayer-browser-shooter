import { vectorAngle } from "./muchMath.js";

export class Projectile{
    constructor(data){
        this.id = data.id;
        this.playerId = data.pId;
        this.velocityX = data.sx;
        this.velocityY = data.sy;
        this.x = data.x;
        this.y = data.y;
        this.rotation = vectorAngle(this.velocityX, this.velocityY);
    }
}
