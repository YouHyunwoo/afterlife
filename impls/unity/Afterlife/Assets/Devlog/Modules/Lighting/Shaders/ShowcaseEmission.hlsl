#ifndef SHOWCASE_EMISSION
#define SHOWCASE_EMISSION
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
half3 ShowcaseEmission(half3 color,half3 vertexColor,half3 baseTint,float crystalGlow,float houseWindows,float night,float clockTime)
{
    half3 palette=vertexColor;
    #if !defined(UNITY_COLORSPACE_GAMMA)
    palette=LinearToSRGB(palette);
    #endif
    float crystal=step(palette.r+.12,min(palette.g,palette.b))*step(.35,min(palette.g,palette.b));
    color+=half3(.12,1,.78)*crystalGlow*lerp(1,.78,night)*crystal*(.96+.04*sin(clockTime*1.5));
    float glazing=houseWindows*step(.85,palette.r)*step(.6,palette.g)*(1-step(.4,palette.b));
    half3 windowTint=max(half3(0,0,0),vertexColor*baseTint);
    windowTint/=max(.001,max(windowTint.r,max(windowTint.g,windowTint.b)));
    return lerp(color,windowTint*2.8*(.985+.015*sin(clockTime*2.3)),glazing*night);
}
#endif
