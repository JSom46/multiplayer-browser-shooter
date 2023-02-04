import { deleteAllChildren } from './utils.js';

class Row{
    constructor(){
        this.bg = new PIXI.Graphics();
        this.row = new PIXI.Container();
        this.fill = 'white';
        this.fontSize = 15;

        return this;
    }

    setBgColor(color){
        this.bgColor = color;

        return this;
    }

    setValues(...values){
        this.values = values;

        return this;
    }

    setDimensions(width, height){
        this.width = width;
        this.height = height;

        return this;
    }

    setPosition(x, y){
        this.row.position.x = x;
        this.row.position.y = y;

        return this;
    }

    setfontSize(fontSize){
        this.fontSize = fontSize;

        return this;
    }

    addToStage(stage){
        this.bg.beginFill(this.bgColor);
        this.bg.drawRect(0, 0, this.width, this.height);
        this.row.addChild(this.bg);

        const cellWidth = this.width / this.values.length;
        const cellHeight = this.height;

        this.values.forEach((val, idx) => {
            const cell = new PIXI.Container();
            cell.addChild(new PIXI.Text(val, {
                fill: this.fill, 
                fontSize: this.fontSize
            }));
            cell.position.x = idx * cellWidth;

            this.row.addChild(cell);
        })

        stage.addChild(this.row);
        this.isAdded = true;

        return this;
    }
}

export class Scoreboard{
    constructor(){
        this.bg = new PIXI.Graphics();
        this.container = new PIXI.Container();
        this.bgColor = 0x1E1E1E;
        this.rows = [];
        this.isAdded = false;

        return this;
    }

    setPlayers(playersArr){
        this.players = playersArr;

        return this;
    }

    setDimensions(width, height){
        this.width = width;
        this.height = height;

        return this;
    }

    setPosition(x, y){
        this.container.position.x = x;
        this.container.position.y = y;

        return this;
    }

    addToStage(stage){
        if(this.isAdded === true){
            return this;
        }

        this.bg.beginFill(this.bgColor);
        this.parent = stage;

        this.bg.drawRect(0, 0, this.width, this.height);
        this.container.addChild(this.bg);

        new Row().setBgColor(0x323232)
            .setDimensions(this.width, 40)
            .setPosition(0, 0)
            .setfontSize(20)
            .setValues('Player name', 'Kills', 'Deaths')
            .addToStage(this.container);

        const sortedPlayers = [...this.players].sort((a, b) => b.kills - a.kills)
        sortedPlayers.forEach((p, idx) => {
            new Row().setBgColor(idx % 2 === 0 ? 0x282828 : 0x1E1E1E)
                .setDimensions(this.width, 20)
                .setPosition(0, 40 + idx * 20)
                .setValues(p.name, p.kills, p.deaths)
                .addToStage(this.container);
        })

        stage.addChild(this.container);

        this.isAdded = true;

        return this;
    }

    deleteFromStage(stage){
        if(this.isAdded === false){
            return this;
        }
        deleteAllChildren(this.container);
        stage.removeChild(this.container);

        this.isAdded = false;

        return this;
    }
};
