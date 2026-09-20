Shader "Afterlife/Prototype/Grass"
{
    Properties
    {

        _VegetationMask("Exclusion mask",2D)="black" {}
        _VegetationBounds("Mask bounds",Vector)=(0,0,1,1)
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
            Blend One Zero
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/_AfterlifePrototype/Shaders/PrototypeVegetationLighting.hlsl"
            #define VEGETATION_OUTPUT(color,world) PrototypeVegetationOutput(color,world)
            #define VEGETATION_SHADE(color,normal,world) PrototypeVegetationShade(color,normal)
            #define GRASS_OUTPUT(color,world,anchor,kind,flower) PrototypeGrassOutput(color,world,anchor,kind,flower)
            #define GRASS_AUTHORED_COLOR(color) PrototypeGrassAuthoredColor(color)
            #define GRASS_NO_HOVER
            #define GRASS_CLIP(anchor) PrototypeGrassClip(anchor)
            #include "Assets/Devlog/Modules/Vegetation/Shaders/ShowcaseGrassPass.hlsl"
            ENDHLSL
        }
    }
}
