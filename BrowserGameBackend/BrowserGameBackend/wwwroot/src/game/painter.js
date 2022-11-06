export class Painter{
    constructor(pixiApp){
        this.app = pixiApp;
        this.app.renderer.backgroundColor = 0x23395D;
        document.getElementById("gameContainer").appendChild(this.app.view);
        this.graphics = PIXI.Graphics;
        this.stage = new PIXI.Container();
        this.app.stage.addChild(this.stage);
        this.cameraCenter = {
            x: this.app.screen.width * 0.5,
            y: this.app.screen.height * 0.5
        };
    }

    setMap(map){
        this.map = map;
        this.tiles = [];
        
        for(let y = 0; y < this.map.tileRows; y++){
            for(let x = 0; x < this.map.tileColumns; x++){
                const frame = new PIXI.Rectangle(
                    this.map.tileMargin + x * (this.map.tileWidth + this.map.tileSpacing),
                    this.map.tileMargin + y * (this.map.tileHeight + this.map.tileSpacing),
                    this.map.tileWidth,
                    this.map.tileHeight
                );

                const tile = new PIXI.Texture(map.tileSet, frame);
                this.tiles.push(tile);
            }
        }
    }

    setDefaultPlayerTexture(texture){
        this.defaultPlayerTexture = texture;
    }

    setDefaultProjectileTexture(texture){
        this.defaultProjectileTexture = texture;
    }

    setPlayers(playersArr){
        this.players = playersArr;
    }

    setProjectiles(projectilesArr){
        this.projectiles = projectilesArr;
    }

    // centar camera on specified object which must contain properties x : number and y : number
    centerCameraOn(object){
        this.cameraCenter = object;
    }

    drawLayer(layeridx){
        const map = new PIXI.Container();
        const layer = this.map.tileMap[layeridx];

        for(let y = 0; y < this.map.height; y++){
            for(let x = 0; x < this.map.width; x++){
                const tile = new PIXI.Sprite(this.tiles[layer[y][x]]);
                tile.position.x = this.map.tileWidth * x;
                tile.position.y = this.map.tileHeight * y;
                map.addChild(tile);
            }
        }
        this.stage.addChild(map)
    }

    drawPlayers(){
        this.players.forEach(p => {
            if(p.sprite == undefined){
                const playerSprite = new PIXI.Sprite(p.texture == undefined ? this.defaultPlayerTexture : p.texture);
                playerSprite.anchor.set(0.5, 0.5);
                p.sprite = playerSprite;
                this.stage.addChild(playerSprite);
            }

            p.sprite.position.x = p.x;
            p.sprite.position.y = p.y;
            p.sprite.rotation = p.rotation;
        });
    }

    drawProjectiles(){
        this.projectiles.forEach(p => {
            if(p.sprite == undefined){
                const projectileSprite = new PIXI.Sprite(p.texture == undefined ? this.defaultProjectileTexture : p.texture);
                projectileSprite.anchor.set(0.5, 0.5);
                p.sprite = projectileSprite;
                this.stage.addChild(projectileSprite);
                p.sprite.rotation = p.rotation;
            }

            p.sprite.position.x = p.x;
            p.sprite.position.y = p.y;
        });
    }

    draw(){
        this.drawLayer(0);
        this.drawLayer(1);
        this.drawPlayers();
        this.drawProjectiles();
        this.drawLayer(2);
        this.drawLayer(3);
    }

    update(){
        const x = 0.5 * this.app.screen.width - this.cameraCenter.x;
        const y = 0.5 * this.app.screen.height - this.cameraCenter.y;

        this.drawPlayers();
        this.drawProjectiles();

        this.stage.position.x = x;
        this.stage.position.y = y;
    }
};
