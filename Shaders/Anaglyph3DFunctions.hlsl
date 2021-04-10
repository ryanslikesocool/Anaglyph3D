float inverseLerp(float from, float to, float value) {
    return (value - from) / (to - from);
}

float remap(float origFrom, float origTo, float targetFrom, float targetTo, float value){
    float rel = inverseLerp(origFrom, origTo, value);
    return lerp(targetFrom, targetTo, rel);
}

float4 equal(float4 x, float4 y) {
  return 1.0 - abs(sign(x - y));
}

float4 notEqual(float4 x, float4 y) {
  return abs(sign(x - y));
}

float4 greaterThan(float4 x, float4 y) {
  return max(sign(x - y), 0.0);
}

float4 lessThan(float4 x, float4 y) {
  return max(sign(y - x), 0.0);
}

float4 greaterEqualThan(float4 x, float4 y) {
  return 1.0 - lessThan(x, y);
}

float4 lessEqualThan(float4 x, float4 y) {
  return 1.0 - greaterThan(x, y);
}