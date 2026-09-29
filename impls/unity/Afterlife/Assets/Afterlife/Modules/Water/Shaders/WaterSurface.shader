Shader "Afterlife/Water/WaterSurface"
{
    Properties
    {
        _ShallowColor("Shallow ocean", Color) = (.10,.56,.76,1)
        _DeepColor("Deep ocean", Color) = (.015,.09,.27,1)
        _FoamColor("Surface foam", Color) = (.94,.99,1,1)
        _LowerFoamColor("Lower foam", Color) = (.35,.76,.94,1)
        _UpperFoamSeed("White foam seed", Float) = 17.3
        _LowerFoamSeed("Blue foam seed", Float) = 83.7
        _FoamScale("Foam pattern size", Range(.5,4)) = 1.7
        _WhiteFoamMinimum("White foam minimum strength", Range(0,1)) = .8
        _WhiteFoamOpacity("White foam layer opacity", Range(0,1)) = .5
        _OpenSeaFoam("Open sea foam", Range(0,1)) = 1
        _DepthRange("Depth darkening distance", Float) = .55
        _ShallowAlpha("Shallow opacity", Range(0,1)) = .60
        _DeepAlpha("Deep opacity", Range(0,1)) = .95
        _FoamWidth("Shore foam width", Float) = .13
        _BottomY("Volume bottom in local space", Float) = -.76
        _RippleDistortion("Ripple distortion distance", Range(0,.2)) = .065
        _SwellStrength("Ambient wave strength", Range(0,1)) = .65
        _SwellSpeed("Ambient wave speed", Range(0,3)) = 1.15
        _SwellLength("Ambient wavelength", Range(.5,8)) = 3.2
        _GridStrength("Water tile grid", Range(0,1)) = .055
        _HoverRipples("Hover ripples", Float) = 1
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent" }
        Pass
        {
            Tags { "LightMode"="SRPDefaultUnlit" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off ZTest LEqual Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Afterlife/Modules/Water/Shaders/WaterSurfacePass.hlsl"
            ENDHLSL
        }
    }
}
