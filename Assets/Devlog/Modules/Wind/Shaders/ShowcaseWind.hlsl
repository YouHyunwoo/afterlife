#ifndef DEVLOG_WIND_INCLUDED
#define DEVLOG_WIND_INCLUDED
float4 _DevlogWind; // 방향 x/z, 공통 시간, 활성 강도
float2 DevlogWindAt(float3 world)
{
    float phase=dot(world.xz,_DevlogWind.xy);
    float gust=smoothstep(.68,1,sin(_DevlogWind.z*.55-phase*.65));
    float strength=.55+.32*sin(_DevlogWind.z*1.6-phase*.8)+.65*gust;
    return _DevlogWind.xy*strength*_DevlogWind.w;
}
#endif
