#ifndef AFTERLIFE_PROTOTYPE_WATER_LIGHTING
#define AFTERLIFE_PROTOTYPE_WATER_LIGHTING
#include "Assets/_AfterlifePrototype/Shaders/PrototypeGroundVisibility.hlsl"
TEXTURE2D(_PrototypeVision); SAMPLER(sampler_PrototypeVision);
float _PrototypeMapRadius, _PrototypeNight;
float4 _VisionSources[128], _VillageLamps[32];
int _VisionCount, _LampCount;
float4 _CursorCircle, _CursorColor;
float _CursorAngle;

half4 PrototypeWaterOutput(half3 color, half opacity, float3 world)
{
    float territorySight=PrototypeTerritoryVision(world.xz,_PrototypeMapRadius);
    float visibility=max(territorySight,PrototypeBuildingVision(world.xz,_PrototypeMapRadius));
    for(int i=0;i<_VisionCount;i++)
        visibility=max(visibility,VisionCircle(world.xz,_VisionSources[i].xy,_VisionSources[i].z,_VisionSources[i].w));
    visibility=smoothstep(0,1,visibility);
    float2 uv=(world.xz+_PrototypeMapRadius)/max(.01,2*_PrototypeMapRadius);
    half territory=SAMPLE_TEXTURE2D(_PrototypeVision,sampler_PrototypeVision,uv).g;
    float2 edge=abs(frac(world.xz+.5)-.5);
    color=lerp(color,color*1.12+half3(.015,.045,.035),territory);
    color=lerp(color,half3(.20,.55,.43),territory*smoothstep(.455,.485,max(edge.x,edge.y))*.65);
    float warm=PrototypeTerritoryLight(world.xz,_PrototypeMapRadius);
    for(int j=0;j<_LampCount;j++)
        warm=max(warm,PrototypeLampInfluence(world,_VillageLamps[j]));
    color=PrototypeNightLighting(color,_PrototypeNight,warm);
    if(_CursorCircle.z>0)
    {
        float2 delta=world.xz-_CursorCircle.xy;
        float d=length(delta);
        float angle=frac((atan2(delta.x,delta.y)-_CursorAngle)/6.2831853+1);
        float ring=(1-smoothstep(.025,.065,abs(d-_CursorCircle.z)))*(angle<=_CursorCircle.w?.72:.22);
        float fill=(1-smoothstep(_CursorCircle.z-.04,_CursorCircle.z,d))*.055;
        color=lerp(color,_CursorColor.rgb,max(fill,ring));
    }
    return half4(color,lerp(1,opacity,visibility));
}
#endif
