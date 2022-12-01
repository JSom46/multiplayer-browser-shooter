class Button{
    constructor(){
        this.button = new PIXI.Container();
        return this;
    }

    setOnClick(handler){
        this.button.on("pointerdown", handler)
        return this;
    }

    setText(text){
        if(this.button.children.length === 0){
            this.button.addChild(new PIXI.Text(text));
            return this;
        }

        this.button.children[0].updateText(text);
        return this;
    }

    addToStage(stage){
        stage.addChild(this.button);
        return this;
    }
}

export class Menu{
    constructor(){
        this.container = new PIXI.Container();
        this.buttons = []
        return this;
    }

    addButton(text, handler){
        this.buttons.push(new Button().setText(text).setOnClick(handler).addToStage(this.container));
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

    addToStage(stage){
        this.parent = stage;
        this.visible = false;
        return this;
    }

    toggle(){
        if(this.visible === false){
            this.parent.addChild(this.container);
        }
        else{
            this.parent.removeChild(this.container)
        }
    }
};
