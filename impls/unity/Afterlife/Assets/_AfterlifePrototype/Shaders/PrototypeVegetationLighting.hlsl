#ifndef PROTOTYPE_VEGETATION_LIGHTING
#define PROTOTYPE_VEGETATION_LIGHTING
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Assets/_AfterlifePrototype/Shaders/PrototypeGroundVisibility.hlsl"
#include "Assets/_AfterlifePrototype/Shaders/PrototypeGroundAppearance.hlsl"
float _PrototypeNight;
float4 _VisionSources[128], _VillageLamps[32];
int _VisionCount, _LampCount;
TEXTURE2D(_VegetationMask); SAMPLER(sampler_VegetationMask);
float4 _VegetationBounds;
void PrototypeGrassClip(float3 anchor)
{
    float2 uv=(anchor.xz-_VegetationBounds.xy)/_VegetationBounds.zw;
    clip(.5-SAMPLE_TEXTURE2D(_VegetationMask,sampler_VegetationMask,uv).r);
}
half3 PrototypeVegetationShade(half3 color,float3 normal)
{
    half light=saturate(dot(normalize(normal),normalize(float3(-.4,.85,-.3))));
    return color*(.55+floor(light*3)*.15);
}
// 풀은 바람에 영향받지 않는 뿌리 좌표로 시야/조명을 계산한다.
half4 PrototypeVegetationOutput(half3 color,float3 visibilityPoint)
{
    float visibility=0;
    for(int i=0;i<_VisionCount;i++)
        visibility=max(visibility,ShowcaseVisionCircle(visibilityPoint.xz,_VisionSources[i].xy,_VisionSources[i].z,_VisionSources[i].w));
    clip(visibility-.001);
    visibility=smoothstep(0,1,visibility);
    float warm=0;
    for(int j=0;j<_LampCount;j++)
        warm=max(warm,saturate(1-distance(visibilityPoint.xz,_VillageLamps[j].xy)/max(.001,_VillageLamps[j].z)));
    color*=PrototypeGroundBrightness(_PrototypeNight,warm);
    color+=half3(.10,.055,.01)*warm*_PrototypeNight;
    return half4(color,1);
}
// 풀 종류별 단색. 높이에 따른 뿌리/끝 색 보간은 사용하지 않는다.
float _GrassUsesTerrainColor;
half4 _GrassBaseColor,_GrassBeigeColor,_GrassDarkColor,_GrassPaleColor;
half4 _GrassTealColor,_GrassMossColor,_GrassPurpleColor,_GrassFlowerPink,_GrassFlowerYellow;
half4 PrototypeGrassOutput(half3 authored,float3 world,float3 anchor,float kind,float flower,float3 normal,float flowerChoice)
{
    half3 baseColor=PrototypeVegetationShade(_GrassBaseColor.rgb,normal);
    half3 color=_GrassUsesTerrainColor>.5?PrototypeGroundColor(anchor):baseColor;
    if(kind>.5 && kind<1.5)color=PrototypeVegetationShade(_GrassBeigeColor.rgb,normal);
    else if(kind>1.5 && kind<2.5)color=PrototypeVegetationShade(_GrassDarkColor.rgb,normal);
    else if(kind>2.5 && kind<3.5)color=PrototypeVegetationShade(_GrassPaleColor.rgb,normal);
    else if(kind>4.5 && kind<5.5)color=PrototypeVegetationShade(_GrassTealColor.rgb,normal);
    else if(kind>5.5 && kind<6.5)color=PrototypeVegetationShade(_GrassMossColor.rgb,normal);
    else if(kind>6.5)color=PrototypeVegetationShade(_GrassPurpleColor.rgb,normal);
    if(kind>3.5 && kind<4.5 && flower>0)
        color=PrototypeVegetationShade(lerp(_GrassFlowerPink.rgb,_GrassFlowerYellow.rgb,flowerChoice),normal);
    return PrototypeVegetationOutput(color,anchor);
}
#endif
