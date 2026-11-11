Shader "Afterlife/Prototype/Grass"
{
    Properties
    {

        _GrassSizeScale("Grass size",Float)=0.555556
        _GrassUsesTerrainColor("Use terrain base",Float)=1
        _GrassBaseColor("BaseColor",Color)=(.30,.40,.23,1)
        _GrassBeigeColor("BeigeColor",Color)=(.65,.57,.41,1)
        _GrassDarkColor("DarkColor",Color)=(.14,.17,.15,1)
        _GrassPaleColor("PaleColor",Color)=(.91,.93,.89,1)
        _GrassTealColor("TealColor",Color)=(.24,.48,.42,1)
        _GrassMossColor("MossColor",Color)=(.36,.43,.20,1)
        _GrassPurpleColor("PurpleColor",Color)=(.47,.30,.48,1)
        _GrassFlowerPink("FlowerPink",Color)=(.82,.66,.78,1)
        _GrassFlowerYellow("FlowerYellow",Color)=(.95,.92,.78,1)
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
            #include "Assets/Prototype/Shaders/PrototypeVegetationLighting.hlsl"
            #define VEGETATION_OUTPUT(color,world) PrototypeVegetationOutput(color,world)
            #define VEGETATION_SHADE(color,normal,world) PrototypeVegetationShade(color,normal)
            #define GRASS_VARIANT_OUTPUT(color,world,anchor,kind,flower,tips,normal,flowerChoice) PrototypeGrassOutput(color,world,anchor,kind,flower,normal,flowerChoice)
            float _GrassSizeScale;
            #define GRASS_SIZE_SCALE _GrassSizeScale
            #define GRASS_CLIP(anchor) PrototypeGrassClip(anchor)
            #include "Assets/Modules/Vegetation/Shaders/GrassPass.hlsl"
            ENDHLSL
        }
    }
}
