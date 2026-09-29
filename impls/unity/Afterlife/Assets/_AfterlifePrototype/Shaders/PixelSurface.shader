Shader "Afterlife/Prototype/PixelSurface"
{
    Properties { _VegetationMask("Decoration exclusion",2D)="black" {} _VegetationBounds("Decoration bounds",Vector)=(0,0,1,1)  _CrystalGlow("Crystal emission",Float)=0 _HouseWindows("Window emission",Float)=0  _Snow("Snow cover",Float)=0 _SnowShell("Snow shell",Float)=0 _SnowPine("Pine snow",Float)=0  _BaseColor("Color", Color) = (1,1,1,1) _Ground("Ground", Float) = 0 _TerrainRelief("Continuous biome terrain", Float) = 0 _Overlay("Overlay", Float) = 0 _Grass("Grass", Float) = 0 _FoliageWind("Tree foliage wind",Float)=0 _GroundDecoration("Ground-linked decoration",Float)=0 _Flash("Hit flash",Float)=0 _Dissolve("Removal",Range(0,1))=0 _DissolveScale("Removal scale",Float)=3.5 _SinkClip("Sink clip",Float)=0 _RemovalFloor("Removal floor",Float)=0 _VertexPalette("Blender vertex colours", Float) = 0 _SrcBlend("Source blend", Float) = 5 _DstBlend("Destination blend", Float) = 10 _ZWrite("Depth write", Float) = 0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Afterlife/Modules/Wind/Shaders/WindField.hlsl"
            #include "Assets/Afterlife/Modules/Lighting/Shaders/SurfaceEmission.hlsl"
            #include "Assets/_AfterlifePrototype/Shaders/PrototypeGroundVisibility.hlsl"
            #include "Assets/_AfterlifePrototype/Shaders/PrototypeGroundAppearance.hlsl"
            #include "Assets/Afterlife/Modules/Hover/Shaders/HoverEffects.hlsl"
            #include "Assets/Afterlife/Modules/Destruction/Shaders/Dissolve.hlsl"
            #include "Assets/Afterlife/Modules/Snow/Shaders/SnowErosion.hlsl"
            float _Snow, _SnowShell;
            int _SnowHitCount;
            float4 _SnowHits[32];
            float4x4 _SnowWorldToLocal;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 ground : TEXCOORD2; float2 decorationAnchor : TEXCOORD3; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 world : TEXCOORD1; half4 color : COLOR; float ground : TEXCOORD3; float3 removalLocal : TEXCOORD4; float2 decorationAnchor : TEXCOORD5; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _CrystalGlow, _HouseWindows;
            float _Flash, _Dissolve, _DissolveScale, _SinkClip, _RemovalFloor;
            float _Ground, _TerrainRelief, _Overlay, _Grass, _VertexPalette, _FoliageWind, _GroundDecoration;
            CBUFFER_END
            int _DamageMarkCountV1;
            float _DamageWood;
            float4 _DamageMarksV1[8],_DamageNormalsV1[8];
            float4x4 _DamageWorldToLocal;
            #include "Assets/Afterlife/Modules/DamageMarks/Shaders/DamageMarks.hlsl"
            TEXTURE2D(_VegetationMask); SAMPLER(sampler_VegetationMask);
            float4 _VegetationBounds;
            float _PrototypeNight;
            float4 _VisionSources[128], _VillageLamps[32];
            int _VisionCount, _LampCount;
            float TerrainHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float TerrainNoise(float2 p)
            {
                float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(TerrainHash(cell),TerrainHash(cell+float2(1,0)),f.x),
                    lerp(TerrainHash(cell+float2(0,1)),TerrainHash(cell+1),f.x),f.y);
            }

            TEXTURE2D(_TerrainTiles); SAMPLER(sampler_TerrainTiles);
            float _TerrainSize, _TerrainRadius;
            float TileType(float2 tile)
            {
                return floor(SAMPLE_TEXTURE2D_LOD(_TerrainTiles,sampler_TerrainTiles,(tile+_TerrainRadius+.5)/_TerrainSize,0).r*255+.5);
            }
            half3 TileColor(float type,float2 world)
            {
                if(type<.5)return half3(.24,.39,.19);
                if(type<1.5)return half3(.42,.30,.19);
                if(type<2.5)return half3(.72,.64,.40);
                if(type<3.5)
                {
                    float waves=sin(world.x*.9+world.y*1.6+_Time.y*.6)*sin(world.y*.7-_Time.y*.35);
                    float crest=smoothstep(.87,.97,sin(world.y*3+sin(world.x*.65)+_Time.y*.7));
                    return lerp(half3(.055,.25,.32),half3(.12,.38,.43),waves*.5+.5)+crest*.022;
                }
                if(type<4.5)return half3(.83,.87,.86);
                if(type<5.5)return half3(.16,.28,.12);
                return half3(.37,.39,.38);
            }
            float IsWater(float type){return abs(type-3)<.25?1:0;}
            float4 _CursorCircle; // x,z,radius,progress
            float4 _CursorColor;
            float _CursorAngle;
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.decorationAnchor=input.decorationAnchor;output.ground=input.ground.x;output.removalLocal=input.positionOS.xyz;
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                if(_Ground>.5 && _TerrainRelief>.5 && input.color.a>=0 && input.color.a<.5)
                    output.world.y+=HoverHeight(output.world);
                if(_Grass>.5)output.world.x+=sin(_Time.y*2+output.world.z*1.7+output.world.x)*.08*saturate(input.positionOS.y+.5);
                half3 foliageColor=_BaseColor.rgb*lerp(half3(1,1,1),input.color.rgb,saturate(_VertexPalette));
                if(_FoliageWind>.5 && foliageColor.g>foliageColor.r*1.08 && foliageColor.g>foliageColor.b*1.05)
                    output.world.xz+=WindAt(output.world)*(.025*saturate(output.world.y*2));
                output.positionCS = TransformWorldToHClip(output.world);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.color = lerp(half4(1,1,1,1),input.color,saturate(_VertexPalette));
                #if !defined(UNITY_COLORSPACE_GAMMA)
                output.color.rgb=pow(max(output.color.rgb,0.0001),2.2);
                #endif
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                if(_GroundDecoration>.5)
                    clip(.5-SAMPLE_TEXTURE2D(_VegetationMask,sampler_VegetationMask,(input.decorationAnchor-_VegetationBounds.xy)/_VegetationBounds.zw).r);
                if(_SinkClip>.5)clip(input.world.y-_RemovalFloor);
                if(_Dissolve>0)clip(DissolveVisibility(input.removalLocal,_DissolveScale,_Dissolve));
                float2 uv = (input.world.xz + _PrototypeMapRadius) / max(.01,2*_PrototypeMapRadius);
                half3 sight = SAMPLE_TEXTURE2D(_PrototypeVision,sampler_PrototypeVision,uv).rgb;
                float territorySight=PrototypeTerritoryVision(input.world.xz,_PrototypeMapRadius);
                float visibility=max(territorySight,PrototypeBuildingVision(input.world.xz,_PrototypeMapRadius));
                for(int i=0;i<_VisionCount;i++)
                {
                    visibility=max(visibility,VisionCircle(input.world.xz,_VisionSources[i].xy,_VisionSources[i].z,_VisionSources[i].w));
                }
                visibility=smoothstep(0,1,visibility);
                half light = saturate(dot(normalize(input.normalWS + .00001), normalize(float3(-.4,.85,-.3))));
                half shade = .55 + floor(light * 3.0) * .15;
                if(_Ground<.5&&_Overlay<.5)clip(visibility-.001);
                bool linkedGround=(_Ground>.5 && _TerrainRelief>.5 && input.color.a>=0 && input.color.a<.5 && input.normalWS.y>.5)
                    || (_GroundDecoration>.5 && input.ground>.5);
                half3 baseColor=_BaseColor.rgb*input.color.rgb;
                if(_DamageMarkCountV1>0)baseColor=DamageSurface(baseColor,mul(_DamageWorldToLocal,float4(input.world,1)).xyz);
                if(_Snow>.5)
                {
                    float3 snowLocal=mul(_SnowWorldToLocal,float4(input.world,1)).xyz;
                    float snowMask=lerp(smoothstep(.22,.66,normalize(input.normalWS).y),1,step(.5,_SnowShell));
                    [loop] for(int snowHit=0;snowHit<_SnowHitCount;snowHit++)snowMask*=1-_SnowHits[snowHit].w*SnowInfluence(snowLocal,_SnowHits[snowHit].xyz);
                    if(_SnowShell>.5){ClipSnow(snowLocal,snowMask);snowMask=smoothstep(.025,.5,snowMask);}
                    baseColor=lerp(baseColor,half3(.87,.94,1)*(.94+TerrainHash(floor(input.world.xz*13))*.06),snowMask);
                }
                baseColor=lerp(baseColor,half3(1,.94,.78),saturate(_Flash));
                if(_Ground>.5 && !linkedGround)
                {
                    float2 p=input.world.xz;
                    float water=0;
                    if(_TerrainRelief>.5)
                    {
                        water=max(0,input.color.a);
                        // 육지 색은 주입된 맵으로 생성한 경계 혼합색, 물은 기존 표현을 유지한다.
                        baseColor=water>.5 ? TileColor(3,p) : input.color.rgb;
                    }
                    else
                    {
                    // 정수 좌표가 타일 중심. 경계 근처만 이웃 타일 ID의 색을 섞는다.
                    float2 cell=floor(p),f=frac(p);
                    float2 blend=smoothstep(.32,.68,f);
                    float a=TileType(cell),b=TileType(cell+float2(1,0)),c=TileType(cell+float2(0,1)),d=TileType(cell+1);
                    baseColor=lerp(lerp(TileColor(a,p),TileColor(b,p),blend.x),lerp(TileColor(c,p),TileColor(d,p),blend.x),blend.y);
                    water=lerp(lerp(IsWater(a),IsWater(b),blend.x),lerp(IsWater(c),IsWater(d),blend.x),blend.y);
                    float shore=water*(1-water)*4;
                    baseColor+=shore*.085*smoothstep(.1,.8,sin(p.y*2+p.x+_Time.y*.7));
                    }
                    // 큰 색 군집과 작은 흙 결을 겹쳐 반복 줄무늬/격자 점을 피한다.
                    float2 warp=float2(TerrainNoise(p*.13+19),TerrainNoise(p*.13-31))*2-1;
                    float2 organic=p+warp*1.8;
                    float broad=TerrainNoise(organic*.24);
                    float medium=TerrainNoise(organic*.73+17);
                    float fine=TerrainNoise(p*3.1+warp);
                    float mottling=(broad-.5)*.15+(medium-.5)*.055+(fine-.5)*.018;
                    baseColor*=1+mottling*(1-water);
                    float soil=smoothstep(.63,.82,TerrainNoise(organic*.41-9));
                    baseColor=lerp(baseColor,baseColor*half3(1.035,.99,.96),soil*(1-water)*.45);
                    float2 edge=abs(frac(input.world.xz+.5)-.5);
                    float tileLine=smoothstep(.455,.485,max(edge.x,edge.y));
                    float territory=(_TerrainRelief>.5 && input.color.a<0)?0:sight.g;
                    baseColor=lerp(baseColor,baseColor*1.12+half3(.015,.045,.035),territory);
                    baseColor=lerp(baseColor,half3(.20,.55,.43),territory*tileLine*.65);
                }
                half3 color = baseColor * shade;
                // Quantize authored colours in display space; linear-space quantization erases dark wood detail.
                #if !defined(UNITY_COLORSPACE_GAMMA)
                if(_VertexPalette>.5)color=pow(floor(pow(saturate(color),1.0/2.2)*31.0+.5)/31.0,2.2);
                else color=floor(saturate(color)*31.0+.5)/31.0;
                #else
                color=floor(saturate(color)*31.0+.5)/31.0;
                #endif
                if(linkedGround)color=PrototypeGroundColor(input.world);
                float warm=PrototypeTerritoryLight(input.world.xz,_PrototypeMapRadius);
                for(int j=0;j<_LampCount;j++)warm=max(warm,PrototypeLampInfluence(input.world,_VillageLamps[j]));
                if(_Overlay<.5)color=PrototypeNightLighting(color,_PrototypeNight,warm);
                if(_Ground>.5&&_CursorCircle.z>0&&(_TerrainRelief<.5||input.color.a>=0))
                {
                    float2 delta=input.world.xz-_CursorCircle.xy;
                    float distanceToCursor=length(delta);
                    float angle=frac((atan2(delta.x,delta.y)-_CursorAngle)/6.2831853+1);
                    float edge=1-smoothstep(.025,.065,abs(distanceToCursor-_CursorCircle.z));
                    float fill=(1-smoothstep(_CursorCircle.z-.04,_CursorCircle.z,distanceToCursor))*.055;
                    float ring=edge*(angle<=_CursorCircle.w?.72:.22);
                    color=lerp(color,_CursorColor.rgb,max(fill,ring));
                }
                if(_Overlay<.5 && _Ground<.5 && _SnowShell<.5)
                    color=SurfaceEmission(color,input.color.rgb,_BaseColor.rgb,_CrystalGlow,_HouseWindows,_PrototypeNight,_Time.y);
                return half4(lerp(color,_BaseColor.rgb,saturate(_Overlay)),(_Ground>.5||_Overlay>.5)?_BaseColor.a:_BaseColor.a*input.color.a);
            }
            ENDHLSL
        }
    }
}
