#ifndef AFTERLIFE_WATER_PASS_INCLUDED
#define AFTERLIFE_WATER_PASS_INCLUDED
#ifndef WATER_LIGHTING
#define WATER_LIGHTING(color,world) DevlogLighting(color,world,1)
#endif
#ifndef WATER_OUTPUT
#define WATER_OUTPUT(color,opacity,world) half4(color,opacity)
#endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Assets/Devlog/Youtube/Modules/Lighting/Shaders/ShowcaseLighting.hlsl"
            #include "Assets/Devlog/Youtube/Modules/Hover/Shaders/ShowcaseHover.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor, _DeepColor, _FoamColor, _LowerFoamColor;
                float _UpperFoamSeed, _LowerFoamSeed, _RippleDistortion, _HoverRipples;
                float _SwellStrength, _SwellSpeed, _SwellLength;
                float _FoamScale, _WhiteFoamMinimum, _WhiteFoamOpacity;
                float _DepthRange, _ShallowAlpha, _DeepAlpha, _FoamWidth, _BottomY, _GridStrength;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 shore : TEXCOORD1;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 world : TEXCOORD0;
                float3 normal : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 shore : TEXCOORD3;
            };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.world);
                output.normal = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.shore = input.shore;
                return output;
            }
            float2 FoamHash(float2 p, float seed)
            {
                p += seed;
                return frac(sin(float2(dot(p,float2(127.1,311.7)),
                                      dot(p,float2(269.5,183.3))))*43758.5453);
            }
            float2 FoamNoise(float2 p, float seed)
            {
                float2 cell = floor(p);
                float2 f = frac(p);
                f = f*f*(3-2*f);
                return lerp(lerp(FoamHash(cell,seed),FoamHash(cell+float2(1,0),seed),f.x),
                            lerp(FoamHash(cell+float2(0,1),seed),FoamHash(cell+1,seed),f.x),f.y);
            }
            // 월드 좌표를 공유하여 물 타일 사이에서 파형이 끊기지 않게 한다.
            float3 AmbientWave(float2 world)
            {
                float frequency = 6.2831853/max(.5,_SwellLength);
                float phase = dot(world,float2(.8,.6))*frequency-_Time.y*_SwellSpeed;
                float crossPhase = dot(world,float2(-.6,.8))*frequency*1.37-_Time.y*_SwellSpeed*.73;
                float2 slope = (float2(.8,.6)*cos(phase)
                    +float2(-.6,.8)*cos(crossPhase)*.35)*_SwellStrength*.32;
                return float3(slope,phase);
            }
            float FoamLayer(float2 p, float seed, float2 drift, float shoreDistance, float contact, float minimumStrength, float surge)
            {
                float2 moving = (p + drift*_Time.y)/max(.1,_FoamScale);
                // 서로 다른 시드의 연속 노이즈로 셀을 왜곡한다. 매 프레임 재추첨하지 않는다.
                float2 warp = (FoamNoise(moving*.95,seed+31.1)-.5)*1.25
                            + (FoamNoise(moving*2.1,seed+57.4)-.5)*.35;
                float2 domain = moving*3.2 + warp;
                float2 cell = floor(domain);
                float2 local = frac(domain);
                float nearest = 8;
                float second = 8;
                [unroll] for (int y=-1;y<=1;y++)
                {
                    [unroll] for (int x=-1;x<=1;x++)
                    {
                        float2 offset = float2(x,y);
                        float2 delta = offset+.15+.7*FoamHash(cell+offset,seed)-local;
                        float distanceSquared = dot(delta,delta);
                        if (distanceSquared < nearest)
                        {
                            second = nearest;
                            nearest = distanceSquared;
                        }
                        else second = min(second,distanceSquared);
                    }
                }
                float breakup = FoamNoise(moving*1.8,seed+113.2).x;
                float width = lerp(.035,.12,breakup)+minimumStrength*.045;
                float cells = 1-smoothstep(width,width+.075,sqrt(second)-sqrt(nearest));
                float shoreWidth = max(.001,_FoamWidth)*surge*lerp(.45,1.65,FoamNoise(moving*2.7,seed+7).y);
                float shore = 1-smoothstep(shoreWidth*.2,shoreWidth,shoreDistance);
                // 타일 경계와 무관한 큰 군집. 물 중앙에는 얇은 흔적과 넓은 여백을 둔다.
                float foamCluster = smoothstep(.43,.72,FoamNoise(p*.55+drift*_Time.y*.2,seed+219.4).x);
                float coast = 1-smoothstep(.08*surge,.48*surge,shoreDistance);
                float coverage = max(max(shore,contact),lerp(.055+.24*foamCluster,.86,coast));
                // 기존 하한은 거품 내부 강도에만 적용한다. 수면 전체를 덮는 하한으로 쓰지 않는다.
                float strength = max(minimumStrength,smoothstep(.24,.64,breakup));
                return saturate(cells*coverage*strength);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // 역투영으로 복원하여 원근/직교 카메라와 저해상도 RT를 모두 처리한다.
                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);
                float deviceDepth = rawDepth;
                #if !UNITY_REVERSED_Z
                    deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, rawDepth);
                #endif
                float3 behind = ComputeWorldSpacePosition(screenUV, deviceDepth, UNITY_MATRIX_I_VP);
                float3 rayDirection = -GetWorldSpaceNormalizeViewDir(input.world);
                float bottomWorld = TransformObjectToWorld(float3(0,_BottomY,0)).y;
                float volumeDistance = max(.001,(input.world.y-bottomWorld)/max(.05,-rayDirection.y));
                float depthGap = max(0,dot(behind-input.world,rayDirection));
                bool hasOpaque = abs(rawDepth-UNITY_RAW_FAR_CLIP_VALUE) > .000001;
                float waterDistance = hasOpaque ? min(depthGap,volumeDistance) : volumeDistance;
                float depthBlend = saturate(1-exp(-waterDistance/max(.01,_DepthRange)*2));
                half3 ocean = lerp(_ShallowColor.rgb,_DeepColor.rgb,depthBlend);
                half opacity = lerp(_ShallowAlpha,_DeepAlpha,depthBlend);

                // 수면에만 해안 거품과 흰 물결을 그린다. 물끼리의 경계에는 해안 거품이 없다.
                float topFace = step(.5,normalize(input.normal).y);
                float3 waveSurface=0;
                float foamClearance=0;
                [branch] if (topFace>0 && _HoverRipples>.5 && _DevlogWaveCount>0)
                    waveSurface=HoverWaveSurface(input.world.xz,foamClearance);
                float3 swell = AmbientWave(input.world.xz);
                waveSurface.xy += swell.xy*topFace;
                float shoreDistance = min(min(lerp(10,input.uv.x,input.shore.x),lerp(10,1-input.uv.x,input.shore.y)),
                                          min(lerp(10,input.uv.y,input.shore.z),lerp(10,1-input.uv.y,input.shore.w)));
                float2 distortion=waveSurface.xy/max(1,length(waveSurface.xy))*_RippleDistortion
                    *smoothstep(0,.12,shoreDistance)*smoothstep(0,.08,depthGap);
                // 굴절과 거품이 같은 파형을 따라 왕복하며, 아래층은 조금 늦게 따라온다.
                float2 foamFlow = swell.xy*.22*topFace;
                float2 pixelWorld = floor((input.world.xz+distortion+foamFlow)*28)/28;
                float contact = hasOpaque ? 1-smoothstep(.025,.10,depthGap) : 0;
                float upperSurge = 1+sin(swell.z)*_SwellStrength*.65;
                float lowerSurge = 1+sin(swell.z-.65)*_SwellStrength*.65;
                float lowerFoam = FoamLayer(pixelWorld-foamFlow*.35,_LowerFoamSeed,float2(-.035,.024),shoreDistance,contact,0,lowerSurge)*topFace;
                float upperFoam = FoamLayer(pixelWorld,_UpperFoamSeed,float2(.027,-.018),shoreDistance,contact,_WhiteFoamMinimum,upperSurge)*topFace;
                upperFoam = min(upperFoam,.9)*_WhiteFoamOpacity;
                // 왜곡 전 월드 원점 마스크로 두 층의 색과 불투명도를 함께 복원한다.
                float foamVisibility=1-saturate(foamClearance);
                lowerFoam *= foamVisibility;
                upperFoam *= foamVisibility;
                float2 edge = min(input.uv,1-input.uv);
                float gridBand = 1-smoothstep(.01,.025,min(edge.x,edge.y));
                ocean *= 1-gridBand*_GridStrength*topFace;
                // 아래 하늘색 층을 먼저 합성하고 흰 거품을 위에 얹는다.
                ocean = lerp(ocean,_LowerFoamColor.rgb,lowerFoam);
                opacity = lerp(opacity,.96,lowerFoam);
                ocean = lerp(ocean,_FoamColor.rgb,upperFoam);
                opacity = lerp(opacity,.94,upperFoam);
                // 접촉선도 원점 주변에서는 거품과 함께 사라졌다 돌아온다.
                float contactHighlight=(1-smoothstep(.018*upperSurge,.075*upperSurge,shoreDistance))*topFace*foamVisibility;
                ocean=lerp(ocean,half3(.70,.94,1),contactHighlight*.85);
                opacity=lerp(opacity,.99,contactHighlight);
                ocean*=1+clamp((-waveSurface.x+waveSurface.y)*.35,-.18,.18)*topFace;
                float ripple=waveSurface.z*topFace;
                ocean=lerp(ocean,half3(.55,.85,.94),ripple*.16);
                opacity=lerp(opacity,.95,ripple*.15);
                half3 color=WATER_LIGHTING(ocean,input.world);
                [branch] if (dot(distortion,distortion)>.00000001 && opacity<.999)
                {
                    // 월드 변위를 투영하여 직교/원근 카메라와 렌더 타깃 비율을 반영한다.
                    float4 shiftedCS=TransformWorldToHClip(input.world+float3(distortion.x,0,distortion.y));
                    float4 shiftedScreen=ComputeScreenPos(shiftedCS);
                    float2 shiftedUV=shiftedScreen.xy/shiftedScreen.w;
                    float2 border=min(screenUV,1-screenUV);
                    shiftedUV=lerp(screenUV,shiftedUV,saturate(min(border.x,border.y)/.025));
                    shiftedUV=clamp(shiftedUV,.001,.999);
                    float shiftedDepth=SampleSceneDepth(shiftedUV);
                    #if !UNITY_REVERSED_Z
                        shiftedDepth=lerp(UNITY_NEAR_CLIP_VALUE,1,shiftedDepth);
                    #endif
                    float3 shiftedBehind=ComputeWorldSpacePosition(shiftedUV,shiftedDepth,UNITY_MATRIX_I_VP);
                    // 앞쪽 육지/오브젝트를 물속으로 끌어오지 않는다.
                    float behindDistance=dot(shiftedBehind-input.world,rayDirection);
                    shiftedUV=lerp(screenUV,shiftedUV,smoothstep(0,.08,behindDistance));
                    half3 background=SampleSceneColor(screenUV);
                    half3 refracted=SampleSceneColor(shiftedUV);
                    // 기존 알파 합성에서 배경 성분만 교체한다. 물과 거품의 불투명도는 유지한다.
                    color+=(refracted-background)*(1-opacity)/max(opacity,.001);
                }
                return WATER_OUTPUT(color,opacity,input.world);
            }

#endif
