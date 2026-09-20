#ifndef SHOWCASE_DISSOLVE_INCLUDED
#define SHOWCASE_DISSOLVE_INCLUDED

// 격자 꼭짓점의 기울기를 보간하는 3D Perlin 노이즈.
// 시간값 없이 원본 오브젝트 좌표를 사용해 이동/회전/바람에도 무늬를 고정한다.
uint DissolveHash(int3 cell)
{
    uint hash=(uint)cell.x*73856093u^(uint)cell.y*19349663u^(uint)cell.z*83492791u;
    hash=(hash^(hash>>13))*1274126177u;
    return hash^(hash>>16);
}

float DissolveGradient(int3 cell,float3 offset)
{
    uint h=DissolveHash(cell)&15u;
    float a=h<8u ? offset.x : offset.y;
    float b=h<4u ? offset.y : (h==12u || h==14u ? offset.x : offset.z);
    return ((h&1u)==0u ? a : -a)+((h&2u)==0u ? b : -b);
}

float DissolvePerlin(float3 position)
{
    int3 cell=(int3)floor(position);
    float3 p=frac(position);
    float3 fade=p*p*p*(p*(p*6-15)+10);
    float a=lerp(DissolveGradient(cell,p),DissolveGradient(cell+int3(1,0,0),p-float3(1,0,0)),fade.x);
    float b=lerp(DissolveGradient(cell+int3(0,1,0),p-float3(0,1,0)),DissolveGradient(cell+int3(1,1,0),p-float3(1,1,0)),fade.x);
    float c=lerp(DissolveGradient(cell+int3(0,0,1),p-float3(0,0,1)),DissolveGradient(cell+int3(1,0,1),p-float3(1,0,1)),fade.x);
    float d=lerp(DissolveGradient(cell+int3(0,1,1),p-float3(0,1,1)),DissolveGradient(cell+int3(1,1,1),p-float3(1,1,1)),fade.x);
    return lerp(lerp(a,b,fade.y),lerp(c,d,fade.y),fade.z);
}

float DissolveField(float3 local,float scale)
{
    float3 p=local*scale+float3(13.17,37.41,7.83);
    float broad=DissolvePerlin(p);
    float detail=DissolvePerlin(p*2.07+float3(31.6,11.2,47.9));
    // 큰 덩어리는 유지하고 경계에만 작은 불규칙성을 더한다.
    return saturate(.5+(broad*.8+detail*.2)*1.2);
}
// 메시 픽셀과 잎 부착점이 같은 임계값으로 제거된다. 0은 보존, 1은 완전 제거.
float DissolveVisibility(float3 local,float scale,float progress)
{
    return progress>0 ? DissolveField(local,max(.01,scale))-lerp(-.001,1.001,progress) : 1;
}
#endif
