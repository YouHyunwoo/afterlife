Shader "Afterlife/Devlog/ShowcaseSurface"
{
    Properties
    {
        _BaseColor("Tint", Color) = (1,1,1,1)
        _VertexPalette("Use existing model colors", Float) = 1
        _Stylized("Stepped lighting", Float) = 1
        _Ground("Ground texture", Float) = 0
        _VisionGround("Keep terrain outside vision",Float)=0
        _Grid("Tile grid", Float) = 0
        _TerrainBlend("Continuous terrain color", Float) = 0
        _TerrainColorMap("Terrain color map", 2D) = "white" {}
        _TerrainMapBounds("Terrain map origin and size", Vector) = (94,-5,12,10)
        _Water("Water surface", Float) = 0
        _Wind("Wind", Float) = 0
        _Flash("Hit flash", Range(0,1)) = 0
        _Hover("Hover", Range(0,1)) = 0
        _Snow("Biome snow cover", Range(0,1)) = 0
        _SnowPine("Full pine crown snow", Float) = 0
        _SnowShell("Separate snow mesh", Float) = 0
        _CrystalGlow("Crystal HDR emission",Float)=0
        _HouseWindows("House window emission", Float) = 0
        _Dissolve("Destruction dissolve", Range(0,1)) = 0
        _DissolveScale("Dissolve Perlin scale", Float) = 3.5
        _SinkClip("Clip sinking geometry", Float) = 0
        _RemovalFloor("Removal ground height", Float) = 0
        _HoverTerrain("Hover terrain deformation", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Modules/Lighting/Shaders/SurfaceLighting.hlsl"
            #include "Assets/Modules/Snow/Shaders/SnowErosion.hlsl"
            #include "Assets/Modules/Lighting/Shaders/SurfaceEmission.hlsl"
            #include "Assets/Modules/Destruction/Shaders/Dissolve.hlsl"
            #include "Assets/Modules/Hover/Shaders/HoverEffects.hlsl"
            #include "Assets/Modules/Wind/Shaders/WindField.hlsl"
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _VertexPalette, _Stylized, _Ground, _Grid, _Water, _Wind, _Flash, _Hover, _Snow, _SnowPine;
            int _SnowHitCount;
            float _HouseWindows, _CrystalGlow;
            float _SnowShell;
            float _Dissolve, _SinkClip, _RemovalFloor;
            float _DissolveScale;
            float _HoverTerrain;
            float _TerrainBlend, _VisionGround;
            float4 _TerrainMapBounds;
            float4 _SnowHits[32];
            float4x4 _SnowWorldToLocal;
            int _DamageMarkCountV1;
            float _DamageWood;
            float4 _DamageMarksV1[8], _DamageNormalsV1[8];
            float4x4 _DamageWorldToLocal;
            CBUFFER_END
            #include "Assets/Modules/DamageMarks/Shaders/DamageMarks.hlsl"
            TEXTURE2D(_TerrainColorMap);
            SAMPLER(sampler_TerrainColorMap);
            struct Attributes { float4 positionOS:POSITION; float3 normalOS:NORMAL; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; half4 color:COLOR; float3 snowLocal:TEXCOORD2; float pine:TEXCOORD3; float3 dissolveLocal:TEXCOORD4; float3 damageLocal:TEXCOORD5; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            void ClipRemoval(float3 world,float3 local)
            {
                if (_SinkClip>.5) clip(world.y-_RemovalFloor);
                if (_Dissolve>0)
                {
                    clip(DissolveVisibility(local,_DissolveScale,_Dissolve));
                }
            }
            float SnowMask(float3 local, float3 normal, float pine)
            {
                float mask = _Snow * lerp(smoothstep(.22,.66,normal.y),1,pine);
                [loop] for (int hit=0;hit<_SnowHitCount;hit++)
                    mask *= 1-_SnowHits[hit].w*SnowInfluence(local,_SnowHits[hit].xyz);
                return mask;
            }
            Varyings Vert(Attributes i)
            {
                Varyings o;
                o.dissolveLocal=i.positionOS.xyz;
                o.world=TransformObjectToWorld(i.positionOS.xyz);
                o.normal=TransformObjectToWorldNormal(i.normalOS);
                o.snowLocal=mul(_SnowWorldToLocal,float4(o.world,1)).xyz;
                o.damageLocal=mul(_DamageWorldToLocal,float4(o.world,1)).xyz;
                // 합쳐진 Tree1 메시에서 초록 팔레트의 잎만 구분하고 갈색 줄기는 유지한다.
                o.pine=_SnowShell;
                // 눈 두께는 별도 닫힌 메시에서 계산한다. 원본 정점은 움직이지 않는다.
                o.world.xz+=WindAt(o.world)*.025*_Wind*saturate(o.world.y*2);
                o.positionCS=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(i.normalOS);
                o.color=lerp(half4(1,1,1,1),i.color,_VertexPalette);
                if (_HoverTerrain>.5)
                {
                    float offset=HoverHeight(o.world);
                    float dx=(HoverHeight(o.world+float3(.008,0,0))-HoverHeight(o.world-float3(.008,0,0)))/.016;
                    float dz=(HoverHeight(o.world+float3(0,0,.008))-HoverHeight(o.world-float3(0,0,.008)))/.016;
                    o.normal=normalize(o.normal-float3(dx,0,dz)*o.normal.y);
                    o.world.y+=offset;
                    o.positionCS=TransformWorldToHClip(o.world);
                }
                #if !defined(UNITY_COLORSPACE_GAMMA)
                o.color.rgb=SRGBToLinear(o.color.rgb);
                #endif
                return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                ClipRemoval(i.world,i.dissolveLocal);
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half ndl=saturate(dot(normalize(i.normal),sun.direction));
                half stepped=floor(ndl*3+.001)/3;
                half shade=lerp(ndl,stepped,_Stylized);
                half3 baseColor=_BaseColor.rgb*i.color.rgb;
                baseColor=DamageSurface(baseColor,i.damageLocal);
                if (_TerrainBlend>.5)
                    baseColor=SAMPLE_TEXTURE2D(_TerrainColorMap,sampler_TerrainColorMap,
                        (i.world.xz-_TerrainMapBounds.xy)/_TerrainMapBounds.zw).rgb;
                // 지형 명암은 실제 굴곡과 넓은 정점색 영역으로 표현한다.
                float wave=sin(i.world.x*2.1+i.world.z*3.4+_Time.y*.8)*sin(i.world.z*2-_Time.y*.5);
                baseColor+=_Water*(wave*.022+step(.91,sin(i.world.z*7+sin(i.world.x*2)+_Time.y))*.035);
                float2 edge=min(frac(i.world.xz),1-frac(i.world.xz));
                float gridLine=1-smoothstep(.015,.035,min(edge.x,edge.y));
                baseColor*=1-_Grid*gridLine*.30;
                // 윗면과 완만한 경사에만 눈이 남고 수직 줄기/아랫면은 원래 색을 유지한다.
                float snowGrain=Hash(floor(i.world.xz*13));
                float snowMask=SnowMask(i.snowLocal,normalize(i.normal),i.pine);
                if (_SnowShell>.5) { ClipSnow(i.snowLocal,snowMask); snowMask=smoothstep(.025,.5,snowMask); }
                baseColor=lerp(baseColor,half3(.87,.94,1)*( .94+snowGrain*.06),snowMask);
                half3 color=baseColor*(half3(.48,.55,.63)+sun.color*shade*.65*sun.shadowAttenuation);
                #if !defined(UNITY_COLORSPACE_GAMMA)
                half3 display=LinearToSRGB(color);
                color=lerp(color,SRGBToLinear(floor(display*31+.5)/31),_Stylized);
                #else
                color=lerp(color,floor(color*31+.5)/31,_Stylized);
                #endif
                color=lerp(color,color*1.18+half3(.025,.018,.004),_Hover);
                color=SurfaceLighting(lerp(color,half3(1,.9,.65),_Flash),i.world,max(_VisionGround,max(_Ground,max(_TerrainBlend,_Water))));
                color=SurfaceEmission(color,i.color.rgb,_BaseColor.rgb,_CrystalGlow,_HouseWindows,_AfterlifeNight,_Time.y);
                return half4(color,1);
            }

            half4 DepthFrag(Varyings i):SV_Target
            {
                ClipVision(i.world,max(_VisionGround,max(_Ground,max(_TerrainBlend,_Water))));
                ClipRemoval(i.world,i.dissolveLocal);
                if (_SnowShell>.5) ClipSnow(i.snowLocal,SnowMask(i.snowLocal,normalize(i.normal),1));
                return 0;
            }
            float3 _LightDirection;
            float3 _LightPosition;
            Varyings ShadowVert(Attributes i)
            {
                Varyings o=Vert(i);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 lightDirection=normalize(_LightPosition-o.world);
                #else
                float3 lightDirection=_LightDirection;
                #endif
                o.positionCS=TransformWorldToHClip(ApplyShadowBias(o.world,normalize(o.normal),lightDirection));
                #if UNITY_REVERSED_Z
                o.positionCS.z=min(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #else
                o.positionCS.z=max(o.positionCS.z,o.positionCS.w*UNITY_NEAR_CLIP_VALUE);
                #endif
                return o;
            }
        ENDHLSL
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Back ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            Cull Back ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            Cull Back ZWrite On ColorMask 0
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
    }
}
