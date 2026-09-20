#ifndef AFTERLIFE_GRASS_PASS
#define AFTERLIFE_GRASS_PASS
#ifndef VEGETATION_OUTPUT
#define VEGETATION_OUTPUT(color,world) half4(DevlogLighting(color,world),1)
#endif
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Devlog/Modules/Lighting/Shaders/ShowcaseLighting.hlsl"
            #include "Assets/Devlog/Modules/Hover/Shaders/ShowcaseHover.hlsl"
            #include "Assets/Devlog/Modules/Wind/Shaders/ShowcaseWind.hlsl"
#ifndef GRASS_OUTPUT
#define GRASS_OUTPUT(color,world,anchor,kind,flower) VEGETATION_OUTPUT(color,world)
#endif
#ifndef GRASS_AUTHORED_COLOR
#define GRASS_AUTHORED_COLOR(color) (color)
#endif
#ifndef GRASS_VARIANT_OUTPUT
#define GRASS_VARIANT_OUTPUT(color,world,anchor,kind,flower,tips,normal,flowerChoice) GRASS_OUTPUT(color,world,anchor,kind,flower)
#endif
#ifndef GRASS_SIZE_SCALE
#define GRASS_SIZE_SCALE 1
#endif
            TEXTURE2D(_Grass0); SAMPLER(sampler_Grass0);
            TEXTURE2D(_Grass1); SAMPLER(sampler_Grass1);
            TEXTURE2D(_Grass2); SAMPLER(sampler_Grass2);
            TEXTURE2D(_Grass3); SAMPLER(sampler_Grass3);
            struct Attributes { float4 positionOS:POSITION; float2 uv:TEXCOORD0; float2 size:TEXCOORD1; float3 type:TEXCOORD2; half4 color:COLOR; float3 normal:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; float2 seed:TEXCOORD2; float3 type:TEXCOORD3; half3 tint:COLOR; float3 normal:TEXCOORD4; float3 anchor:TEXCOORD5; };
            float GrassAlpha(float2 uv,float index)
            {
                // 원본 PNG는 보존하고 투명 여백을 제외한 UV만 샘플링한다.
                if (index<.5) return SAMPLE_TEXTURE2D(_Grass0,sampler_Grass0,(float2(4,4)+uv*float2(9,9))/16).a;
                if (index<1.5) return SAMPLE_TEXTURE2D(_Grass1,sampler_Grass1,(float2(5,3)+uv*float2(5,4))/16).a;
                if (index<2.5) return SAMPLE_TEXTURE2D(_Grass2,sampler_Grass2,(float2(1,4)+uv*float2(14,9))/16).a;
                return SAMPLE_TEXTURE2D(_Grass3,sampler_Grass3,(float2(4,2)+uv*float2(8,14))/16).a;
            }
            float2 Gradient(float2 p)
            {
                float a=frac(sin(dot(p,float2(127.1,311.7)))*43758.5453)*6.2831853;
                return float2(cos(a),sin(a));
            }
            float Perlin(float2 p)
            {
                float2 cell=floor(p), f=frac(p), u=f*f*f*(f*(f*6-15)+10);
                float a=dot(Gradient(cell),f), b=dot(Gradient(cell+float2(1,0)),f-float2(1,0));
                float c=dot(Gradient(cell+float2(0,1)),f-float2(0,1)), d=dot(Gradient(cell+1),f-1);
                return lerp(lerp(a,b,u.x),lerp(c,d,u.x),u.y);
            }
            Varyings Vert(Attributes i)
            {
                i.size*=GRASS_SIZE_SCALE;
                Varyings o;
                float3 anchor=TransformObjectToWorld(i.positionOS.xyz);
                o.anchor=anchor;
                // 잎별 잔떨림 시드는 로컬 좌표, 큰 바람은 공통 월드 좌표를 사용한다.
                o.seed=i.positionOS.xz;
                o.type=i.type; o.tint=i.color.rgb;
                #if !defined(UNITY_COLORSPACE_GAMMA)
                o.tint=SRGBToLinear(o.tint);
                #endif
                o.normal=TransformObjectToWorldNormal(i.normal);
                float3 right=normalize(float3(UNITY_MATRIX_I_V._m00,0,UNITY_MATRIX_I_V._m20));
                float3 away=normalize(cross(right,float3(0,1,0)));
                float2 flow=DevlogWindAt(anchor);
                float2 timeFlow=_DevlogWind.xy*_DevlogWind.z*.32;
                float flutter=Perlin(o.seed*2.3-timeFlow+13);
                float sway=(dot(flow,right.xz)*.11+flutter*.04)*i.size.y/.3;
                float tilt=dot(flow,away.xz)*.42+Perlin(o.seed*.6-timeFlow+47)*.22;
                // y=0에서 좌우/앞뒤 변위는 0. 윗변 두 점은 동일한 좌우 이동량을 쓴다.
                float depth=sin(tilt)*i.size.y*i.uv.y;
                float perspective=1/(1+depth*1.4);
                float horizontal=(i.uv.x-.5)*i.size.x*perspective+sway*i.uv.y;
                float height=cos(tilt)*i.size.y*i.uv.y*perspective;
                o.world=anchor+right*horizontal+float3(0,height,0)+away*depth;
                #ifndef GRASS_NO_HOVER
                o.world+=HoverGrass(anchor,i.size.y,i.uv.y);
                #endif
                o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                #ifdef GRASS_CLIP
                GRASS_CLIP(i.anchor);
                #endif
                float mask=GrassAlpha(i.uv,i.type.z)-.5;
                float flower=0;
                if (i.type.y>3.5 && i.type.y<4.5)
                {
                    float2 flowerDelta=(i.uv-float2(.5,.77))*float2(1,1.25);
                    float angle=atan2(flowerDelta.y,flowerDelta.x);
                    flower=.135+.035*cos(angle*5)-length(flowerDelta);
                    float stem=min(.022-abs(i.uv.x-.5),.77-i.uv.y);
                    mask=max(stem,flower);
                }
                clip(mask);
                float tips=smoothstep(.1,.8,i.uv.y);
                half3 color=i.tint*lerp(1,i.type.x,tips);
                half3 accent=color;
                if (i.type.y>.5 && i.type.y<1.5) accent=GRASS_AUTHORED_COLOR(half3(.38,.29,.14));
                else if (i.type.y>1.5 && i.type.y<2.5) accent=GRASS_AUTHORED_COLOR(half3(.018,.025,.020));
                else if (i.type.y>2.5 && i.type.y<3.5) accent=GRASS_AUTHORED_COLOR(half3(.80,.84,.76));
                if (i.type.y>.5 && i.type.y<3.5) color=lerp(color,accent,tips);
                if (i.type.y>4.5)
                {
                    half3 forestTip=i.type.y<5.5 ? i.tint*half3(.65,1.08,1.15)
                        : i.type.y<6.5 ? i.tint*half3(.82,.86,.55) : i.tint*half3(1.2,.70,1.28);
                    color=lerp(color,forestTip,tips*.8);
                }
                if (i.type.y>3.5 && i.type.y<4.5 && flower>0)
                    color=lerp(GRASS_AUTHORED_COLOR(half3(.64,.39,.57)),GRASS_AUTHORED_COLOR(half3(.90,.83,.57)),step(0,Perlin(i.seed*3+23)));
                // 뿌리의 기준색과 지면 법선으로 바닥과 같은 광원을 받는다.
                #ifdef VEGETATION_SHADE
                color=VEGETATION_SHADE(color,i.normal,i.world);
                #else
                Light sun=GetMainLight(TransformWorldToShadowCoord(i.world));
                half ndl=saturate(dot(normalize(i.normal),sun.direction));
                half shade=lerp(ndl,floor(ndl*3+.001)/3,.25);
                color*=half3(.48,.55,.63)+sun.color*shade*.65*sun.shadowAttenuation;
                #endif
                #if !defined(UNITY_COLORSPACE_GAMMA)
                half3 display=LinearToSRGB(color);
                color=lerp(color,SRGBToLinear(floor(display*31+.5)/31),.25);
                #else
                color=lerp(color,floor(color*31+.5)/31,.25);
                #endif
                return GRASS_VARIANT_OUTPUT(color,i.world,i.anchor,i.type.y,flower,tips,i.normal,step(0,Perlin(i.seed*3+23)));
            }
#endif
