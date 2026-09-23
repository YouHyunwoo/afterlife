#ifndef SHOWCASE_VISION_INCLUDED
#define SHOWCASE_VISION_INCLUDED
float ShowcaseVisionCircle(float2 positionXZ,float2 center,float radius,float strength)
{
    return radius>0?saturate((radius-distance(positionXZ,center))/.65)*strength:0;
}
half ShowcaseVisionBrightness(float night,float visibility)
{
    return lerp(lerp(.48,.15,night),1,visibility);
}
#endif
