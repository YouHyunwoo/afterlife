#ifndef DEVLOG_LIGHTING_INCLUDED
#define DEVLOG_LIGHTING_INCLUDED
float _DevlogNight;
float4 _DevlogCursorVision, _DevlogHouseVision;
float DevlogVision(float3 world, float4 source)
{
    // 바깥 반경은 고정하고 감쇠 시작점만 안쪽으로 옮긴다.
    return source.w > 0 ? 1-smoothstep(max(0,source.w-1.8),source.w,distance(world,source.xyz)) : 0;
}
half3 DevlogLighting(half3 color, float3 world)
{
    float house=DevlogVision(world,_DevlogHouseVision);
    float visible=max(house,DevlogVision(world,_DevlogCursorVision));
    // 시야 밖은 거의 어둡게 숨긴다. 시야 안의 밝기와 경계 감쇠는 유지한다.
    half3 nightColor=color*lerp(half3(.018,.025,.045),half3(.56,.64,.80),visible);
    return lerp(color,nightColor,_DevlogNight);
}
#endif
