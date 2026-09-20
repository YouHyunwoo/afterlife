Shader "Afterlife/Devlog/HoverDust"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Devlog/Modules/Lighting/Shaders/ShowcaseLighting.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float3 world:TEXCOORD1; };
            V Vert(A i) { V o; o.world=TransformObjectToWorld(i.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world); o.uv=i.uv; o.color=i.color; return o; }
            half4 Frag(V i):SV_Target
            {
                float mask=1-smoothstep(.1,.5,length(i.uv-.5));
                return half4(DevlogLighting(i.color.rgb,i.world),i.color.a*mask);
            }
            ENDHLSL
        }
    }
}
