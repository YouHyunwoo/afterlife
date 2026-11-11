Shader "Afterlife/Threshold/Portal"
{
    Properties { _MainTex("Village",2D)="black"{} _Visibility("Visibility",Range(0,1))=1 _Clock("Clock",Float)=0 _Motion("Motion",Float)=1 _Reflection("Reflection",Float)=0 _Gloss("Specular power",Float)=72 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha Cull Off ZWrite Off
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
                        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "ThresholdPuddle.cginc"
            #include "ThresholdPortalShape.hlsl"
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            float _Visibility,_Clock,_Motion,_Reflection,_Gloss;
            struct Attributes { float4 positionOS:POSITION;float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1; };
            Varyings vert(Attributes v)
            {
                Varyings o;o.uv=v.uv;
                if(_Reflection<.5) v.positionOS.z+=portalHeight(v.uv,_Clock*_Motion,_Motion);
                o.world=TransformObjectToWorld(v.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.world);return o;
            }
            half4 frag(Varyings i):SV_Target
            {
                float2 uv=i.uv;if(_Reflection>.5)uv.y=1-uv.y;
                float t=_Clock*_Motion; float edge=portalEdge(uv,t,_Motion);
                float aa=max(fwidth(edge),.002);
                float inside=1-smoothstep(-aa,aa,edge);
                float bleed=(1-smoothstep(.01,.095,edge))*saturate(thresholdNoise((uv*2-1)*32+t*.025)*1.5);
                float2 warp=float2(sin(uv.y*24+t*.73)+sin(uv.y*43-t*.4)*.3,sin(uv.x*18-t*.55))*.012*_Motion;
                float3 village=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,clamp(uv+warp,.01,.99)).rgb;
                float recovery=pow(smoothstep(.008,.28,-edge),1.35);
                float3 c=village*recovery*inside;
                // The portal membrane stays dark; deformation is independent of scene lighting.
                float alpha=max(inside,bleed)*_Visibility;
                if(_Reflection>.5)
                {
                    clip(-puddleDistance(puddlePoint(i.world)));
                    float peak=max(c.r,max(c.g,c.b));c*=smoothstep(.82,1,peak)*.22;
                    alpha*=pow(saturate(i.uv.y),2)*.5;
                }
                return half4(c*_Visibility,alpha);
            }
            ENDHLSL
        }
    }
}
