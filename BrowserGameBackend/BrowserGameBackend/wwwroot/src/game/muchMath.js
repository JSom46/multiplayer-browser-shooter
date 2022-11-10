export const vectorAngle = (x, y) => {
    // zero vector
    if(x == 0 && y == 0){
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
