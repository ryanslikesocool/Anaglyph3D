float inverseLerp(float from, float to, float value) {
    return (value - from) / (to - from);
}

float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value){
    float rel = inverseLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}