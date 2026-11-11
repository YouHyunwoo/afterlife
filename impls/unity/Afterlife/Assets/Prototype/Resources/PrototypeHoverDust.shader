Shader "Afterlife/Prototype/HoverDust"
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
            #include "Assets/Prototype/Shaders/PrototypeVegetationLighting.hlsl"
            #define HOVER_DUST_LIGHTING(color,world) PrototypeVegetationOutput(color,world).rgb
            #include "Assets/Modules/Hover/Shaders/HoverDustPass.hlsl"
            ENDHLSL
        }
    }
}
