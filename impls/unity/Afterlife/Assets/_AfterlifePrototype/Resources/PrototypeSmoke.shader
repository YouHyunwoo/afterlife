Shader "Afterlife/Prototype/Smoke"
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_AfterlifePrototype/Shaders/PrototypeVegetationLighting.hlsl"
            #define SHOWCASE_SMOKE_COLOR(color,world) PrototypeVegetationOutput(color,world).rgb
            #include "Assets/Devlog/Modules/Lighting/Shaders/ShowcaseSmokePass.hlsl"
            ENDHLSL
        }
    }
}
