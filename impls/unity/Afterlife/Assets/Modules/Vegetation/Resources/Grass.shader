Shader "Afterlife/Vegetation/Grass"
{
    Properties
    {
        _GrassSizeScale("Grass size",Float)=1
        _Grass0("Grass PNG 1",2D)="white" {}
        _Grass1("Grass PNG 2",2D)="white" {}
        _Grass2("Grass PNG 3",2D)="white" {}
        _Grass3("Grass PNG 4",2D)="white" {}
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            float _GrassSizeScale;
            #define GRASS_SIZE_SCALE _GrassSizeScale
            #include "Assets/Modules/Vegetation/Shaders/GrassPass.hlsl"
            ENDHLSL
        }
    }
}
