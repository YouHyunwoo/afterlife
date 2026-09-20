#ifndef DEVLOG_LIGHTING_INCLUDED
#define DEVLOG_LIGHTING_INCLUDED
#include "Assets/Devlog/Modules/Lighting/Shaders/ShowcaseVision.hlsl"
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
    return smoothstep(0,1,visible);
}
void DevlogClipVision(float3 world,float ground)
{
    if(_DevlogVisionTest>.5 && ground<.5)clip(DevlogTestVisibility(world)-.001);
}
half3 DevlogLighting(half3 color,float3 world,float ground)
{
    if(_DevlogVisionTest>.5)
    {
        float visible=DevlogTestVisibility(world);
        DevlogClipVision(world,ground);
        // 지형은 밤에도 남기고, 오브젝트는 시야 밖에서 깊이와 함께 제거한다.
        return color*ShowcaseVisionBrightness(_DevlogNight,visible);
    }
    float house=DevlogVision(world,_DevlogHouseVision);
    float visible=max(house,DevlogVision(world,_DevlogCursorVision));
    // 시야 밖은 거의 어둡게 숨긴다. 시야 안의 밝기와 경계 감쇠는 유지한다.
    half3 nightColor=color*lerp(half3(.018,.025,.045),half3(.56,.64,.80),visible);
    return lerp(color,nightColor,_DevlogNight);
}
half3 DevlogLighting(half3 color,float3 world)
{
    return DevlogLighting(color,world,0);
}
#endif
