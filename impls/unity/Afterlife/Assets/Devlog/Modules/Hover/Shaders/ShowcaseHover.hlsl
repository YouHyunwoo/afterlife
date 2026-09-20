#ifndef SHOWCASE_HOVER_INCLUDED
#define SHOWCASE_HOVER_INCLUDED
float _DevlogHoverTime;
float4 _DevlogHoverOrigin, _DevlogGrassCursor;
int _DevlogGrassStampCountV3, _DevlogTerrainStampCountV3;
float4 _DevlogGrassStampsV3[96], _DevlogTerrainStampsV3[96];
float _DevlogTerrainWeightsV3[96];
TEXTURE2D(_DevlogHoverBiomes); SAMPLER(sampler_DevlogHoverBiomes);
float HoverKind(float2 cell)
{
    return round(SAMPLE_TEXTURE2D_LOD(_DevlogHoverBiomes,sampler_DevlogHoverBiomes,(cell+float2(6.5,5.5))/float2(12,10),0).r*3);
}
float HoverHeight(float3 world)
{
    if (_DevlogTerrainStampCountV3==0) return 0;
    float3 p=world-_DevlogHoverOrigin.xyz;
    float2 cell=floor(p.xz),f=frac(p.xz);
    if (p.x< -6 || p.x>=6 || p.z< -5 || p.z>=5) return 0;
    float kind=HoverKind(cell);
    if (kind<1.5) return 0;
    float mask=1;
    if (HoverKind(cell-float2(1,0))!=kind) mask=min(mask,smoothstep(0,.10,f.x));
    if (HoverKind(cell+float2(1,0))!=kind) mask=min(mask,smoothstep(0,.10,1-f.x));
    if (HoverKind(cell-float2(0,1))!=kind) mask=min(mask,smoothstep(0,.10,f.y));
    if (HoverKind(cell+float2(0,1))!=kind) mask=min(mask,smoothstep(0,.10,1-f.y));
    float depression=0,ridge=0;
    [loop] for (int i=0;i<_DevlogTerrainStampCountV3;i++)
    {
        float4 stamp=_DevlogTerrainStampsV3[i];
        float age=_DevlogHoverTime-stamp.z;
        if (stamp.w!=kind || age<0 || age>2.5) continue;
        float2 delta=world.xz-stamp.xy;
        if (dot(delta,delta)>.25) continue;
        float r=length(delta)/.28;
        float fade=_DevlogTerrainWeightsV3[i];
        depression=max(depression,exp(-r*r*4)*fade);
        ridge=max(ridge,exp(-(r-.9)*(r-.9)*25)*fade);
    }
    return (-.09*depression+.018*ridge)*mask*saturate((p.y+.8)/.4);
}
float3 HoverGrass(float3 anchor,float height,float tip)
{
    float2 offset=anchor.xz-_DevlogGrassCursor.xy;
    float distance=length(offset);
    float2 direction=offset/max(.03,distance);
    float strength=(1-smoothstep(.08,.7,distance))*_DevlogGrassCursor.z;
    float2 bend=0;
    if (_DevlogGrassCursor.w<.5) bend=direction*strength;
    else
    {
        [loop] for (int i=0;i<_DevlogGrassStampCountV3;i++)
        {
            float4 stamp=_DevlogGrassStampsV3[i]; float age=_DevlogHoverTime-stamp.z;
            if (stamp.w!=0 || age<0 || age>1.2) continue;
            float2 delta=anchor.xz-stamp.xy;
            float weight=1-smoothstep(.05,.65,length(delta));
            bend+=delta/max(.04,length(delta))*weight*sin(age*17)*exp(-age*4)*.65;
        }
    }
    float magnitude=length(bend);
    bend/=max(1,magnitude);
    return float3(bend.x,-min(1,magnitude)*.35,bend.y)*height*tip*.85;
}
// CPU에서 수명 감쇠를 한 번 계산하고 활성 물결을 압축한다.
int _DevlogWaveCount;
float4 _DevlogWaves[32];
float _DevlogRingFades[32];
// 픽셀 전용 가상 기울기와 동심원 강도. 실제 표면 높이는 변경하지 않는다.
float3 HoverWaveSurface(float2 world, out float foamClearance)
{
    float height=0,rings=0;
    float2 slope=0;
    foamClearance=0;
    [loop] for (int i=0;i<_DevlogWaveCount;i++)
    {
        float4 wave=_DevlogWaves[i];
        float2 delta=world-wave.xy;
        float radiusSquared=dot(delta,delta);
        // UploadWaves의 반경(age * .85)에서 나이를 복원한다.
        // 이동하는 고리의 범위 검사 전에 원점 주변 거품을 처리한다.
        float age=wave.z/.85;
        float clearing=smoothstep(0,.08,age)*(1-smoothstep(.30,1.20,age));
        float originMask=1-smoothstep(.16*.16,.38*.38,radiusSquared);
        foamClearance=max(foamClearance,originMask*clearing);
        float outer=wave.z+.5,inner=max(0,wave.z-.5);
        if (radiusSquared>outer*outer || radiusSquared<inner*inner) continue;
        float radius=sqrt(radiusSquared);
        float phase=radius-wave.z;
        float sine,cosine;
        sincos(phase*24,sine,cosine);
        float envelope=exp(-phase*phase*32)*wave.w;
        height+=sine*envelope;
        slope+=delta/max(radius,.0001)*envelope*(24*cosine-64*phase*sine);
        float first=phase/.045,second=(phase+.14)/.035;
        rings+=(exp(-first*first)+.4*exp(-second*second))*_DevlogRingFades[i];
    }
    if (abs(height)>=.06) slope=0;
    return float3(slope,saturate(rings));
}
// 기존 물결 소비자는 거품 마스크 없이 같은 결과를 받는다.
float3 HoverWaveSurface(float2 world)
{
    float foamClearance;
    return HoverWaveSurface(world,foamClearance);
}
float HoverRipple(float2 world) { return HoverWaveSurface(world).z; }
#endif
