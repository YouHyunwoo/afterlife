#ifndef AFTERLIFE_LEAF_PASS
#define AFTERLIFE_LEAF_PASS
#ifndef VEGETATION_OUTPUT
#define VEGETATION_OUTPUT(color,world) half4(SurfaceLighting(color,world),1)
#endif
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
   #include "Assets/Afterlife/Modules/Lighting/Shaders/SurfaceLighting.hlsl"
   #include "Assets/Afterlife/Modules/Wind/Shaders/WindField.hlsl"
   #include "Assets/Afterlife/Modules/Snow/Shaders/SnowErosion.hlsl"
   #include "Assets/Afterlife/Modules/Destruction/Shaders/Dissolve.hlsl"
   CBUFFER_START(UnityPerMaterial)
   half4 _BaseColor;
   float _Hover, _Flash, _LeafRotationVariation;
   float _LeafSnow, _LeafSnowPine;
   float _Dissolve, _DissolveScale, _SinkClip, _RemovalFloor;
   int _SnowHitCount;
   float4 _SnowHits[32];
   float4x4 _SnowWorldToLocal;
   CBUFFER_END
   TEXTURE2D(_Leaf); SAMPLER(sampler_Leaf);
   struct A { float4 positionOS:POSITION; float3 normal:NORMAL; float2 uv:TEXCOORD0; float4 size:TEXCOORD1; half4 color:COLOR; };
   struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 uv:TEXCOORD1; float3 normal:TEXCOORD2; half3 color:COLOR; float3 snowLocal:TEXCOORD3; float exposure:TEXCOORD4; float removal:TEXCOORD5; };
   V Vert(A i)
   {
    V o;
    // 카드 네 점 모두 같은 수관 로컬 부착점을 사용한다. 빌보드/바람 변위는 제외한다.
    o.removal=DissolveVisibility(i.positionOS.xyz,_DissolveScale,_Dissolve);
    float3 anchor=TransformObjectToWorld(i.positionOS.xyz);
    float3 n=TransformObjectToWorldNormal(i.normal);
    // 카메라의 화면 평면과 평행한 완전 빌보드. 법선은 부착 위치와 조명에만 사용한다.
    float3 right=normalize(float3(UNITY_MATRIX_I_V._m00,UNITY_MATRIX_I_V._m10,UNITY_MATRIX_I_V._m20));
    float3 up=normalize(float3(UNITY_MATRIX_I_V._m01,UNITY_MATRIX_I_V._m11,UNITY_MATRIX_I_V._m21));
    float angle=i.size.z;
    // 위상은 유지하고 화면 평면 안에서만 고정된 랜덤 회전을 적용한다.
    float rotation=angle*_LeafRotationVariation;
    float3 rotatedRight=right*cos(rotation)+up*sin(rotation);
    up=-right*sin(rotation)+up*cos(rotation); right=rotatedRight;
    // 고정 회전은 유지하고 작은 잔떨림 위에 공통 바람 방향을 더한다.
    float wave=sin(_AfterlifeWind.z*2.7+angle*2)*.014;
    float2 flow=WindAt(anchor);
    o.world=anchor+n*.018+right*((i.uv.x-.5)*i.size.x+wave*i.uv.y)+up*(i.uv.y*i.size.y+wave*i.uv.y*.3);
    // 수관 메시의 바람 변위를 먼저 따라간 뒤 각 잎 끝만 추가로 흔든다.
    o.world.xz+=flow*(.025*saturate(anchor.y*2)+.06*i.uv.y);
    o.exposure=i.size.w;
    o.snowLocal=mul(_SnowWorldToLocal,float4(anchor,1)).xyz;
    o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; o.normal=n; o.color=i.color.rgb;
    #if !defined(UNITY_COLORSPACE_GAMMA)
    o.color=SRGBToLinear(o.color);
    #endif
    return o;
   }
   half4 Frag(V i):SV_Target
   {
    clip(i.removal);
    if (_SinkClip>.5) clip(i.world.y-_RemovalFloor);
    float snow=_LeafSnow*i.exposure;
    [loop] for (int hit=0;hit<_SnowHitCount;hit++)
        snow*=1-_SnowHits[hit].w*SnowInfluence(i.snowLocal,_SnowHits[hit].xyz);
    float leafAlpha=SAMPLE_TEXTURE2D(_Leaf,sampler_Leaf,(float2(1,4)+i.uv*float2(14,9))/16).a;
    // 잎 알파의 위쪽으로 눈을 확장해 얇은 하얀 도색이 아닌 두꺼운 테두리를 만든다.
    float2 snowUV=i.uv-float2(0,.10*snow);
    float snowAlpha=SAMPLE_TEXTURE2D(_Leaf,sampler_Leaf,(float2(1,4)+snowUV*float2(14,9))/16).a;
    float coverage=smoothstep(.05,.45,snow)*lerp(smoothstep(.12,.55,i.uv.y),1,_LeafSnowPine);
    clip(max(leafAlpha,snowAlpha*coverage)-.5);
    Light sun=GetMainLight();
    float shade=saturate(dot(normalize(i.normal),sun.direction));
    half3 tint=lerp(i.color,half3(.87,.94,1),coverage*saturate(snowAlpha+leafAlpha));
    #ifdef VEGETATION_SHADE
    half3 color=VEGETATION_SHADE(tint*_BaseColor.rgb,i.normal,i.world);
    #else
    half3 color=tint*(half3(.48,.55,.63)+sun.color*shade*.65);
    #endif
    color=lerp(color,color*1.18+half3(.025,.018,.004),_Hover);
    color=lerp(color,half3(1,.9,.65),_Flash);
    return VEGETATION_OUTPUT(color,i.world);
   }
#endif
