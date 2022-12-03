import { vectorAngle } from "./utils.js";

export class Projectile{
    constructor(data, timestamp){
        const delta = timestamp === undefined ? 0 : performance.now - timestamp;
        this.id = data.id;
        this.playerId = data.pId;
        this.velocityX = data.sx;
        this.velocityY = data.sy;
        this.x = data.x + this.velocityX * delta;
        this.y = data.y + this.velocityY * delta;
        this.rotation = vectorAngle(this.velocityX, this.velocityY);
    }
}
