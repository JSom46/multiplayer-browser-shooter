export class TileMap{
    constructor(data, tileSet){
        const tileset = [];

        data.tilesets[0].tiles.forEach(e => {
            tileset.push({id: e.id, collides: e.properties[0].value});
        });

        this.tileSet = tileSet;
        this.traversabilityMap = [];
        this.shootThroughMap = [];
        this.tileMap = [[], [], [], []];
        this.width = data.width;
        this.height = data.height;
        this.tileWidth = data.tilesets[0].tilewidth;
        this.tileHeight = data.tilesets[0].tileheight;
        this.tileMargin = data.tilesets[0].margin;
        this.tileSpacing = data.tilesets[0].spacing;
        this.tileColumns = data.tilesets[0].columns;
        this.tileRows = Math.round(data.tilesets[0].tilecount / data.tilesets[0].columns);
        this.playerHitboxRadius = Math.max(this.tileWidth, this.tileHeight) * 0.4;
        this.projectileHitboxRadius = Math.max(this.tileWidth, this.tileHeight) * 0.25;

        for(let y = 0; y < data.height; y++){
            this.traversabilityMap.push(new Array(data.width));
            this.shootThroughMap.push(new Array(data.width));
            this.tileMap[0].push(new Array(data.width));
            this.tileMap[1].push(new Array(data.width));
            this.tileMap[2].push(new Array(data.width));
            this.tileMap[3].push(new Array(data.width));
        }

        for(let y = 0; y < data.height; y++){
            for (let x = 0; x < data.width; x++) {
                this.traversabilityMap[y][x] = !((data.layers[0].data[y * data.width + x] - 1 < 0 ? true : tileset[data.layers[0].data[y * data.width + x] - 1].collides) || (data.layers[2].data[y * data.width + x] - 1 < 0 ? false : tileset[data.layers[2].data[y * data.width + x] - 1].collides));
                this.shootThroughMap[y][x] = !(data.layers[2].data[y * data.width + x] - 1 < 0 ? false : tileset[data.layers[2].data[y * data.width + x] - 1].collides);
                this.tileMap[0][y][x] = data.layers[0].data[y * data.width + x] - 1;
                this.tileMap[1][y][x] = data.layers[1].data[y * data.width + x] - 1;
                this.tileMap[2][y][x] = data.layers[2].data[y * data.width + x] - 1;
                this.tileMap[3][y][x] = data.layers[3].data[y * data.width + x] - 1;
            }
        }
    }
};
