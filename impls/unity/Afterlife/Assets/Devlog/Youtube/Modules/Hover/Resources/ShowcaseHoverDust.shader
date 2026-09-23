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
            #include "Assets/Devlog/Youtube/Modules/Lighting/Shaders/ShowcaseLighting.hlsl"
            #include "Assets/Devlog/Youtube/Modules/Hover/Shaders/ShowcaseHoverDustPass.hlsl"
            ENDHLSL
        }
    }
}
