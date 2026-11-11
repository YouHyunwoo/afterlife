#ifndef AFTERLIFE_VISION_INCLUDED
#define AFTERLIFE_VISION_INCLUDED
float VisionCircle(float2 positionXZ,float2 center,float radius,float strength)
{
    return radius>0?saturate((radius-distance(positionXZ,center))/.65)*strength:0;
}
half VisionBrightness(float night,float visibility)
{
    return lerp(lerp(.48,.15,night),1,visibility);
}
// 조명이 적용된 색에 곱한다. 낮밤·광원 반경과 독립적으로 시야 밖을 20% 어둡게 한다.
half3 ApplyVisionDimming(half3 litColor,float visibility)
{
    return litColor*lerp(.8,1,saturate(visibility));
}
#endif
