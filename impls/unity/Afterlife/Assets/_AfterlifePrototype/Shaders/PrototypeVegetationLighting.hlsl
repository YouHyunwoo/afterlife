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
half3 PrototypeGrassAuthoredColor(half3 color)
{
    #if !defined(UNITY_COLORSPACE_GAMMA)
    return SRGBToLinear(color);
    #else
    return color;
    #endif
}
// 풀은 바람에 영향받지 않는 뿌리 좌표로 시야/조명을 계산한다.
half4 PrototypeVegetationOutput(half3 color,float3 visibilityPoint)
{
    float visibility=0;
    for(int i=0;i<_VisionCount;i++)
        visibility=max(visibility,ShowcaseVisionCircle(visibilityPoint.xz,_VisionSources[i].xy,_VisionSources[i].z,_VisionSources[i].w));
    clip(visibility-.001);
    visibility=smoothstep(0,1,visibility);
    // Day01과 같은 밝기 감쇠와 시야 밖 제거만 사용한다.
    color*=PrototypeGroundBrightness(_PrototypeNight,visibility);
    float warm=0;
    for(int j=0;j<_LampCount;j++)
        warm=max(warm,saturate(1-distance(visibilityPoint.xz,_VillageLamps[j].xy)/max(.001,_VillageLamps[j].z)));
    color+=half3(.10,.055,.01)*warm*_PrototypeNight*visibility;
    return half4(color,1);
}
// 일반 풀은 별도 어두운 변종/끝 색을 쓰지 않고 지면의 최종 기준색을 공유한다.
half4 PrototypeGrassOutput(half3 authored,float3 world,float3 anchor,float kind,float flower)
{
    half3 color=PrototypeGroundColor(anchor);
    if(kind>3.5 && kind<4.5 && flower>0)color=lerp(color,authored,.7);
    return PrototypeVegetationOutput(color,anchor);
}
#endif
