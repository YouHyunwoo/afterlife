#ifndef PROTOTYPE_GROUND_VISIBILITY
#define PROTOTYPE_GROUND_VISIBILITY
#include "Assets/Afterlife/Modules/Lighting/Shaders/Vision.hlsl"
TEXTURE2D(_PrototypeBuildingSight); SAMPLER(sampler_PrototypeBuildingSight);
TEXTURE2D(_PrototypeTerritorySight); SAMPLER(sampler_PrototypeTerritorySight);
SAMPLER(sampler_TerritoryLinearClamp);
float PrototypeTerritoryLight(float2 positionXZ, float mapRadius)
{
    float2 uv = (positionXZ + mapRadius) / max(.01, 2 * mapRadius);
    if (any(uv < 0) || any(uv > 1)) return 0;
    return SAMPLE_TEXTURE2D(_PrototypeTerritorySight, sampler_TerritoryLinearClamp, uv).g;
}
float PrototypeTerritoryVision(float2 positionXZ, float mapRadius)
{
    float2 uv = (positionXZ + mapRadius) / max(.01, 2 * mapRadius);
    if (any(uv < 0) || any(uv > 1)) return 0;
    return SAMPLE_TEXTURE2D(_PrototypeTerritorySight, sampler_PrototypeTerritorySight, uv).r;
}
// C# unions every completed building footprint into this mask; moving sources remain circles.
float PrototypeBuildingVision(float2 positionXZ, float mapRadius)
{
    float2 uv = (positionXZ + mapRadius) / max(.01, 2 * mapRadius);
    return SAMPLE_TEXTURE2D(_PrototypeBuildingSight, sampler_PrototypeBuildingSight, uv).r;
}
// Day01 SurfaceLighting과 같은 밤 색 및 집 조명 감쇠를 사용한다.
float PrototypeLampInfluence(float3 world,float4 lamp)
{
    return lamp.w>0 ? 1-smoothstep(max(0,lamp.w-1.8),lamp.w,distance(world,lamp.xyz)) : 0;
}
half3 PrototypeNightLighting(half3 color,float night,float illumination)
{
    half3 nightColor=color*lerp(half3(.018,.025,.045),half3(.56,.64,.80),saturate(illumination));
    return lerp(color,nightColor,saturate(night));
}
#endif
