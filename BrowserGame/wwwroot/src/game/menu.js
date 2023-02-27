class Button{
    constructor(){
        this.bg = new PIXI.Graphics();
        this.button = new PIXI.Container();
        return this;
    }

    setOnClick(handler){
        this.button.interactive = true;
        this.button.on("pointerdown", handler)
        return this;
    }

    setText(text){
        if(this.button.children.length === 0){
            this.text = text;
            
            return this;
        }

        this.button.children[0].updateText(text);
        return this;
    }

    addToStage(stage){
        this.bg.beginFill(0xFFFFFF);
        this.bg.alpha = 0.7;
        this.bg.drawRect(0, 0, this.width, this.height);

        this.button.addChild(this.bg);
        this.button.addChild(new PIXI.Text(this.text));

        this.button.on('mouseover', () => {
            this.bg.alpha = 1;
        });

        this.button.on('mouseout', () => {
            this.bg.alpha = 0.7;
        });
        
        stage.addChild(this.button);
        return this;
    }

    setDimensions(width, height){
        this.width = width;
        this.height = height;

        return this;
    }

    setPosition(x, y){
        this.button.position.x = x;
        this.button.position.y = y;

        return this;
    }
}

export class Menu{
    constructor(){
        this.bg = new PIXI.Graphics();
        this.container = new PIXI.Container();
        this.buttons = [];
        this.bgColor = 0x1E1E1E;
        return this;
    }

    addButton(text, handler){
        this.buttons.push(new Button().setText(text).setOnClick(handler).setDimensions(this.width - 30, 50));
        return this;
    }

    deleteButton(idx){
        if(idx >= this.buttons.length){
            throw {msg: "Index out of bounds."};
        }

        this.container.removeChild(this.buttons[idx]);
        this.buttons.splice(idx, 1);

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
        this.bg.beginFill(this.bgColor);
        this.parent = stage;

        this.bg.drawRect(0, 0, this.width, this.height);
        this.container.addChild(this.bg);

        this.buttons.forEach((b, idx) => {
            b.setPosition(15, 15 + 60 * idx).addToStage(this.container);
        })

        this.visible = false;
        return this;
    }

    toggle(){
        if(this.visible === false){
            this.parent.addChild(this.container);
            this.visible = true;
        }
        else{
            this.parent.removeChild(this.container)
            this.visible = false;
        }
    }
};
