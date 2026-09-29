Shader "Afterlife/Threshold/Pool"
{
    Properties { _WaveHeight("Wave height",Float)=.035 _WaveWidth("Wave width",Float)=.12 _Gloss("Specular power",Float)=96 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #include "ThresholdSpecular.hlsl"
            #include "ThresholdPuddle.cginc"
            float _WaveHeight,_WaveWidth,_Gloss;
            struct Attributes { float4 positionOS:POSITION; };
            struct Varyings { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 puddlePosition:TEXCOORD1; };
            float randomValue(float seed) { return frac(sin(seed*127.1+311.7)*43758.5453); }
            // x = height, yz = its analytic derivatives. No emissive ring is drawn.
            float3 wave(float2 p,float birth)
            {
                float angle=randomValue(birth)*6.283185;
                float radius=sqrt(randomValue(birth+73.19))*.72;
                float2 center=float2(0,-2)+float2(cos(angle)*2.5,sin(angle)*5)*radius;
                float phase=saturate((_Time.y-birth*3.2)/8);
                float2 delta=p-center; float r=length(delta);
                float x=r-phase*2.2; float width=max(_WaveWidth,.02);
                float h=_WaveHeight*exp(-x*x/(width*width))*smoothstep(0,.06,phase)*(1-smoothstep(.88,1,phase));
                float2 gradient=(-2*x/(width*width))*h*delta/max(r,.001);
                return float3(h,gradient);
            }
            float3 waves(float2 p)
            {
                float birth=floor(_Time.y/3.2);
                return wave(p,birth)+wave(p,birth-1)+wave(p,birth-2);
            }
            Varyings vert(Attributes v)
            {
                Varyings o; o.world=TransformObjectToWorld(v.positionOS.xyz);
                o.puddlePosition=puddlePoint(o.world);
                float3 surface=waves(o.puddlePosition);
                float3 up=normalize(unity_ObjectToWorld._m01_m11_m21);
                o.world+=up*surface.x;
                o.positionCS=TransformWorldToHClip(o.world); return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                clip(-puddleDistance(i.puddlePosition));
                float3 surface=waves(i.puddlePosition);
                float3 normal=TransformObjectToWorldNormal(normalize(float3(-surface.y,1,-surface.z)));
                // Suppress flat water; gently admit reflection only as the normal tilts from world up.
                float tilt=1-saturate(dot(normal,float3(0,1,0)));
                float waveReflection=smoothstep(.00015,.006,tilt);
                return half4(thresholdSpecular(i.world,normal,_Gloss)*waveReflection,1);
            }
            ENDHLSL
        }
    }
}
