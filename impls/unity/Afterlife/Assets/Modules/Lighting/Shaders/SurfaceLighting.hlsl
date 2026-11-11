#ifndef DEVLOG_LIGHTING_INCLUDED
#define DEVLOG_LIGHTING_INCLUDED
#include "Assets/Modules/Lighting/Shaders/Vision.hlsl"
float _AfterlifeNight, _AfterlifeVisionTest, _AfterlifeCursorFade;
float4 _AfterlifeCursorVision, _AfterlifeHouseVision;
float4 _AfterlifeVisionTrail[32];
int _AfterlifeVisionTrailCount;
float VisionInfluence(float3 world, float4 source)
{
    // 바깥 반경은 고정하고 감쇠 시작점만 안쪽으로 옮긴다.
    return source.w > 0 ? 1-smoothstep(max(0,source.w-1.8),source.w,distance(world,source.xyz)) : 0;
}
float TestVisibility(float3 world)
{
    float radius=_AfterlifeCursorVision.w;
    float visible=VisionCircle(world.xz,_AfterlifeCursorVision.xz,radius,_AfterlifeCursorFade);
    [loop] for(int i=0;i<_AfterlifeVisionTrailCount;i++)
    {
        float4 trail=_AfterlifeVisionTrail[i];
        visible=max(visible,VisionCircle(world.xz,trail.xy,trail.z,trail.w));
    }
    // 집은 마우스와 별개의 시야를 제공한다.
    visible=max(visible,VisionCircle(world.xz,_AfterlifeHouseVision.xz,_AfterlifeHouseVision.w,1));
    return smoothstep(0,1,visible);
}
void ClipVision(float3 world,float ground)
{
    if(_AfterlifeVisionTest>.5 && ground<.5)clip(TestVisibility(world)-.001);
}
half3 SurfaceLighting(half3 color,float3 world,float ground)
{
    // 물체 노출과 조명은 각각 계산한 뒤 시야 밖 어두움을 누적한다.
    ClipVision(world,ground);
    // 집 조명만 밤의 밝기를 올린다. 커서는 시야 원점이며 광원이 아니다.
    float4 houseLamp=_AfterlifeHouseVision;
    houseLamp.w*=.65;
    float houseLight=VisionInfluence(world,houseLamp);
    half3 nightColor=color*lerp(half3(.018,.025,.045),half3(.56,.64,.80),houseLight);
    half3 litColor=lerp(color,nightColor,_AfterlifeNight);
    return _AfterlifeVisionTest>.5 ? ApplyVisionDimming(litColor,TestVisibility(world)) : litColor;
}
half3 SurfaceLighting(half3 color,float3 world)
{
    return SurfaceLighting(color,world,0);
}
#endif
