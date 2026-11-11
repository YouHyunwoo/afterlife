#ifndef HOVER_DUST_LIGHTING
#define HOVER_DUST_LIGHTING(color,world) SurfaceLighting(color,world)
#endif
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float3 world:TEXCOORD1; };
            V Vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; o.color=i.color; return o; }
            half4 Frag(V i):SV_Target
            {
                float mask=1-smoothstep(.1,.5,length(i.uv-.5));
                return half4(HOVER_DUST_LIGHTING(i.color.rgb,i.world),i.color.a*mask);
            }
