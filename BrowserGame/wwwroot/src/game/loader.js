import { TileMap } from "./map.js";

export class Loader{
    constructor(){
        this.loader = PIXI.Loader.shared
    }

    getTexture(key){
        return this.loader.resources[key].texture;
    }

    // loads tilemap and tileset
    loadMap(name){
        return new Promise(resolve => {
            this.loader.add([{name: "mapJson", url: `../../assets/maps/${name}.json`}, {name: "tileSet", url: `../../assets/maps/${name}.png`}])
            .load(out => {
                resolve(new TileMap(out.resources["mapJson"].data, out.resources["tileSet"].texture));
            });
        });
    }

    // loads texture located at ../../assets/textures/{path} and saves it with a name {name}
    loadTexture(name){
        return new Promise(resolve => {
            this.loader.add([{name: name, url: `../../assets/textures/${name}.png`}])
            .load(out => {
                resolve(out.resources[name].texture);
            })
        });
    }
}
