Shader "Afterlife/Prototype/PixelSurface"
{
    Properties { _BaseColor("Color", Color) = (1,1,1,1) _Ground("Ground", Float) = 0 _TerrainRelief("Continuous biome terrain", Float) = 0 _Overlay("Overlay", Float) = 0 _Grass("Grass", Float) = 0 _VertexPalette("Blender vertex colours", Float) = 0 _SrcBlend("Source blend", Float) = 5 _DstBlend("Destination blend", Float) = 10 _ZWrite("Depth write", Float) = 0 }
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
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float3 world : TEXCOORD1; half4 color : COLOR; };
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            float _Ground, _TerrainRelief, _Overlay, _Grass, _VertexPalette;
            CBUFFER_END
            TEXTURE2D(_PrototypeVision); SAMPLER(sampler_PrototypeVision);
            float _PrototypeMapRadius, _PrototypeNight;
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
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                if(_Grass>.5)output.world.x+=sin(_Time.y*2+output.world.z*1.7+output.world.x)*.08*saturate(input.positionOS.y+.5);
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
                float2 uv = (input.world.xz + _PrototypeMapRadius) / max(.01,2*_PrototypeMapRadius);
                half3 sight = SAMPLE_TEXTURE2D(_PrototypeVision,sampler_PrototypeVision,uv).rgb;
                float visibility=0;
                for(int i=0;i<_VisionCount;i++)
                {
                    float d=distance(input.world.xz,_VisionSources[i].xy);
                    visibility=max(visibility,saturate((_VisionSources[i].z-d)/.65)*_VisionSources[i].w);
                }
                visibility=smoothstep(0,1,visibility);
                half light = saturate(dot(normalize(input.normalWS + .00001), normalize(float3(-.4,.85,-.3))));
                half shade = .55 + floor(light * 3.0) * .15;
                if(_Ground<.5&&_Overlay<.5)clip(visibility-.001);
                half3 baseColor=_BaseColor.rgb*input.color.rgb;
                if(_Ground>.5)
                {
                    float2 p=input.world.xz;
                    float water=0;
                    if(_TerrainRelief>.5)
                    {
                        water=input.color.a;
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
                    baseColor=lerp(baseColor,baseColor*1.12+half3(.015,.045,.035),sight.g);
                    baseColor=lerp(baseColor,half3(.20,.55,.43),sight.g*tileLine*.65);
                }
                half3 color = baseColor * shade;
                // Quantize authored colours in display space; linear-space quantization erases dark wood detail.
                #if !defined(UNITY_COLORSPACE_GAMMA)
                if(_VertexPalette>.5)color=pow(floor(pow(saturate(color),1.0/2.2)*31.0+.5)/31.0,2.2);
                else color=floor(saturate(color)*31.0+.5)/31.0;
                #else
                color=floor(saturate(color)*31.0+.5)/31.0;
                #endif
                // 지면은 낮에 약간 어둡게 남고 밤에는 암흑. 물체는 clip되어 완전히 숨는다.
                if(_Ground>.5)color*=lerp(lerp(.48,0,_PrototypeNight),1,visibility);
                float warm=0;
                for(int j=0;j<_LampCount;j++)warm=max(warm,saturate(1-distance(input.world.xz,_VillageLamps[j].xy)/_VillageLamps[j].z));
                color+=half3(.10,.055,.01)*warm*_PrototypeNight*visibility;
                if(_Ground>.5&&_CursorCircle.z>0)
                {
                    float2 delta=input.world.xz-_CursorCircle.xy;
                    float distanceToCursor=length(delta);
                    float angle=frac((atan2(delta.x,delta.y)-_CursorAngle)/6.2831853+1);
                    float edge=1-smoothstep(.025,.065,abs(distanceToCursor-_CursorCircle.z));
                    float fill=(1-smoothstep(_CursorCircle.z-.04,_CursorCircle.z,distanceToCursor))*.055;
                    float ring=edge*(angle<=_CursorCircle.w?.72:.22);
                    color=lerp(color,_CursorColor.rgb,max(fill,ring));
                }
                return half4(lerp(color,_BaseColor.rgb,saturate(_Overlay)),(_Ground>.5||_Overlay>.5)?_BaseColor.a:visibility*_BaseColor.a*input.color.a);
            }
            ENDHLSL
        }
    }
}
