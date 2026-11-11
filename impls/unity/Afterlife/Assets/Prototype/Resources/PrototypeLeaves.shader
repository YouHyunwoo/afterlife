Shader "Afterlife/Prototype/Leaves"
{
 Properties {
 _BaseColor("Tint",Color)=(1,1,1,1) _Leaf("Leaf sprite",2D)="white" {} _Hover("Hover",Float)=0 _Flash("Flash",Float)=0 _LeafRotationVariation("Random leaf rotation",Range(0,1))=0
 _Dissolve("Destruction dissolve",Range(0,1))=0 _DissolveScale("Dissolve Perlin scale",Float)=3.5
 _SinkClip("Clip sinking leaves",Float)=0 _RemovalFloor("Removal ground height",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  Pass
  {
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
            #include "Assets/Modules/Vegetation/Shaders/LeafCardsPass.hlsl"
            ENDHLSL
  }
 }
}
