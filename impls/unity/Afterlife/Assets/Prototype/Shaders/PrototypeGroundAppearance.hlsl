#ifndef PROTOTYPE_GROUND_APPEARANCE
#define PROTOTYPE_GROUND_APPEARANCE
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Assets/Afterlife/Modules/Hover/Shaders/HoverEffects.hlsl"
TEXTURE2D(_PrototypeGroundColors); SAMPLER(sampler_PrototypeGroundColors);
TEXTURE2D(_PrototypeGroundNormals); SAMPLER(sampler_PrototypeGroundNormals);
TEXTURE2D(_PrototypeVision); SAMPLER(sampler_PrototypeVision);
float4 _PrototypeGroundBounds;
float _PrototypeMapRadius;
half3 PrototypeGroundColor(float3 world)
{
    float2 uv=(world.xz-_PrototypeGroundBounds.xy)/_PrototypeGroundBounds.zw;
    half3 color=SAMPLE_TEXTURE2D(_PrototypeGroundColors,sampler_PrototypeGroundColors,uv).rgb;
    float3 normal=normalize(SAMPLE_TEXTURE2D(_PrototypeGroundNormals,sampler_PrototypeGroundNormals,uv).rgb*2-1);
    if (_AfterlifeTerrainStampCountV3>0)
    {
        float dx=(HoverHeight(world+float3(.008,0,0))-HoverHeight(world-float3(.008,0,0)))/.016;
        float dz=(HoverHeight(world+float3(0,0,.008))-HoverHeight(world-float3(0,0,.008)))/.016;
        normal=normalize(normal+float3(-dx,0,-dz));
    }
    float light=saturate(dot(normal,normalize(float3(-.4,.85,-.3))));
    // Day01과 같이 지면과 식생이 같은 연속 명암과 약한 색 단계화를 사용한다.
    float shade=lerp(light,floor(light*3+.001)/3,.25);
    color*=half3(.48,.55,.63)+half3(1,.95,.85)*shade*.65;
    float2 sightUV=(world.xz+_PrototypeMapRadius)/max(.01,2*_PrototypeMapRadius);
    half territory=SAMPLE_TEXTURE2D(_PrototypeVision,sampler_PrototypeVision,sightUV).g;
    color=lerp(color,color*1.12+half3(.015,.045,.035),territory);
    #if !defined(UNITY_COLORSPACE_GAMMA)
    color=lerp(color,SRGBToLinear(floor(LinearToSRGB(color)*31+.5)/31),.25);
    #else
    color=lerp(color,floor(color*31+.5)/31,.25);
    #endif
    return color;
}
#endif
