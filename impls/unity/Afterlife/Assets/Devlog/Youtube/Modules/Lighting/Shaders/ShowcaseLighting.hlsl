#ifndef DEVLOG_LIGHTING_INCLUDED
#define DEVLOG_LIGHTING_INCLUDED
#include "Assets/Devlog/Youtube/Modules/Lighting/Shaders/ShowcaseVision.hlsl"
float _DevlogNight, _DevlogVisionTest, _DevlogCursorFade;
float4 _DevlogCursorVision, _DevlogHouseVision;
float4 _DevlogVisionTrail[32];
int _DevlogVisionTrailCount;
float DevlogVision(float3 world, float4 source)
{
    // 바깥 반경은 고정하고 감쇠 시작점만 안쪽으로 옮긴다.
    return source.w > 0 ? 1-smoothstep(max(0,source.w-1.8),source.w,distance(world,source.xyz)) : 0;
}
float DevlogTestVisibility(float3 world)
{
    float radius=_DevlogCursorVision.w;
    float visible=ShowcaseVisionCircle(world.xz,_DevlogCursorVision.xz,radius,_DevlogCursorFade);
    [loop] for(int i=0;i<_DevlogVisionTrailCount;i++)
    {
        float4 trail=_DevlogVisionTrail[i];
        visible=max(visible,ShowcaseVisionCircle(world.xz,trail.xy,trail.z,trail.w));
    }
    // 집은 마우스와 별개의 시야를 제공한다.
    visible=max(visible,ShowcaseVisionCircle(world.xz,_DevlogHouseVision.xz,_DevlogHouseVision.w,1));
    return smoothstep(0,1,visible);
}
void DevlogClipVision(float3 world,float ground)
{
    if(_DevlogVisionTest>.5 && ground<.5)clip(DevlogTestVisibility(world)-.001);
}
half3 DevlogLighting(half3 color,float3 world,float ground)
{
    // 시야 테스트는 물체의 표시 여부만 바꾸고 낮/밤 밝기는 그대로 유지한다.
    DevlogClipVision(world,ground);
    // 집 조명만 밤의 밝기를 올린다. 커서는 시야 원점이며 광원이 아니다.
    float4 houseLamp=_DevlogHouseVision;
    houseLamp.w*=.65;
    float houseLight=DevlogVision(world,houseLamp);
    half3 nightColor=color*lerp(half3(.018,.025,.045),half3(.56,.64,.80),houseLight);
    return lerp(color,nightColor,_DevlogNight);
}
half3 DevlogLighting(half3 color,float3 world)
{
    return DevlogLighting(color,world,0);
}
#endif
