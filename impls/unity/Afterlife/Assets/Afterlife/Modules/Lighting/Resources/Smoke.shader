Shader "Afterlife/Lighting/Smoke"
{
    Properties { _EmitterY("Chimney height", Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+10" "RenderType"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off ZTest LEqual
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Afterlife/Modules/Lighting/Shaders/SmokePass.hlsl"
            ENDHLSL
        }
    }
}
