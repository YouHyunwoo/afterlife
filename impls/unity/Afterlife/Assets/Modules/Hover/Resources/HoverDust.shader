Shader "Afterlife/Hover/HoverDust"
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
            #include "Assets/Modules/Lighting/Shaders/SurfaceLighting.hlsl"
            #include "Assets/Modules/Hover/Shaders/HoverDustPass.hlsl"
            ENDHLSL
        }
    }
}
