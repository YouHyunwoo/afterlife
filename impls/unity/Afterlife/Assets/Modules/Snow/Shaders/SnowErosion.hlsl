#ifndef DEVLOG_SNOW_EROSION_INCLUDED
#define DEVLOG_SNOW_EROSION_INCLUDED
float SnowHash(int3 cell)
{
    uint h=(uint)cell.x*73856093u ^ (uint)cell.y*19349663u ^ (uint)cell.z*83492791u;
    h=(h^(h>>13))*1274126177u;
    return (h&65535u)/65535.0;
}
float SnowNoise(float3 p)
{
    int3 c=(int3)floor(p); float3 f=frac(p); f=f*f*(3-2*f);
    return lerp(lerp(lerp(SnowHash(c),SnowHash(c+int3(1,0,0)),f.x),lerp(SnowHash(c+int3(0,1,0)),SnowHash(c+int3(1,1,0)),f.x),f.y),
        lerp(lerp(SnowHash(c+int3(0,0,1)),SnowHash(c+int3(1,0,1)),f.x),lerp(SnowHash(c+int3(0,1,1)),SnowHash(c+int3(1,1,1)),f.x),f.y),f.z);
}
float SnowInfluence(float3 local,float3 hit)
{
    float d=length((local-hit)*float3(1,.88,1.08))+(SnowNoise(local*8+hit*3)-.5)*.16+(SnowNoise(local*23)-.5)*.045;
    return 1-smoothstep(.08,.44,d);
}
void ClipSnow(float3 local,float remaining)
{
    float coverage=smoothstep(.025,.40,remaining);
    clip(coverage-(.015+.97*SnowHash((int3)floor(local*95))));
}
#endif
