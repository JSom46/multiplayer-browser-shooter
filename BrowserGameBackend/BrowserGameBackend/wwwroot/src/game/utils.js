// returns angle between x axis and vactor (x, y)
export const vectorAngle = (x, y) => {
    // zero vector
    if(x === 0 && y === 0){
        return 0;
    }
    // first quadrant
    if(x >= 0 && y >= 0){
        return Math.atan(y / x);
    }
    // second quadrant
    if(x <= 0 && y >= 0){
        return Math.atan(y / x) + Math.PI;
    }
    // third quadrant
    if(x < 0 && y <= 0){
        return Math.atan(y / x) + Math.PI;
    }
    // fourth quadrant
    if(x >= 0 && y <= 0){
        return Math.atan(y / x) + 2 * Math.PI;
    }
};

// direction of player's movement:
// 0 - upper-left, 1 - up, 2 - upper-right
// 3 - left, 4 - no movement, 5 - right
// 4 - bottom-left, 5 - down, 6 - bottom-right
export const movementDirection = (x, y) => {
    if(x === -1 && y === -1) return 0;
    if(x === 0 && y === -1) return 1;
    if(x === 1 && y === -1) return 2;
    if(x === -1 && y === 0) return 3;
    if(x === 0 && y === 0) return 4;
    if(x === 1 && y === 0) return 5;
    if(x === -1 && y === 1) return 6;
    if(x === 0 && y === 1) return 7;
    if(x === 1 && y === 1) return 8;
};

// deletes all children of stage object
export const deleteAllChildren = (stage) => {
    for(let i = stage.children.length - 1; i >= 0; i--){
        stage.removeChild(stage.children[i]);
    }
};