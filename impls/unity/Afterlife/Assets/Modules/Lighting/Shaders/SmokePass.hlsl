#ifndef AFTERLIFE_SMOKE_PASS
#define AFTERLIFE_SMOKE_PASS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #ifndef AFTERLIFE_SMOKE_COLOR
            #include "Assets/Modules/Lighting/Shaders/SurfaceLighting.hlsl"
            #define AFTERLIFE_SMOKE_COLOR(color,world) SurfaceLighting(color,world)
            #endif
            CBUFFER_START(UnityPerMaterial)
            float _EmitterY;
            CBUFFER_END
            struct Attributes { float4 vertex:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct Varyings { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; float3 world:TEXCOORD1; half4 color:COLOR; };
            float Hash(float2 p) { return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float Noise(float2 p)
            {
                float2 cell=floor(p), f=frac(p); f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),
                    lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            Varyings Vert(Attributes i)
            {
                Varyings o; o.world=TransformObjectToWorld(i.vertex.xyz);
                o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; o.color=i.color; return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float2 p=i.uv*3.8+float2(_Time.y*.09,-_Time.y*.16)+i.world.xz*.17;
                float2 warp=float2(Noise(p+17.1),Noise(p-23.7))-.5;
                float density=Noise(p+warp*.9)*.6+Noise(p*2.1+warp)*.28+Noise(p*4.3)*.12;
                float radial=1-smoothstep(.15,.5,length(i.uv-.5));
                float heightFade=1-smoothstep(1.1,2.2,i.world.y-_EmitterY);
                float alpha=smoothstep(.24,.70,density)*radial*heightFade*i.color.a;
                return half4(AFTERLIFE_SMOKE_COLOR(i.color.rgb,i.world),alpha);
            }

#endif
