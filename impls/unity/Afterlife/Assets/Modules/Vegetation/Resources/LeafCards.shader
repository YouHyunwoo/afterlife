Shader "Afterlife/Vegetation/LeafCards"
{
 Properties { _Leaf("Leaf sprite",2D)="white" {} _Hover("Hover",Float)=0 _Flash("Flash",Float)=0 _LeafRotationVariation("Random leaf rotation",Range(0,1))=0
 _Dissolve("Destruction dissolve",Range(0,1))=0 _DissolveScale("Dissolve Perlin scale",Float)=3.5
 _SinkClip("Clip sinking leaves",Float)=0 _RemovalFloor("Removal ground height",Float)=0 }
 SubShader
 {
  Tags { "RenderPipeline"="UniversalPipeline" "Queue"="AlphaTest" "RenderType"="TransparentCutout" }
  Pass
  {
   Cull Off ZWrite On
   HLSLPROGRAM
   #pragma target 3.5
   #pragma vertex Vert
   #pragma fragment Frag
            #include "Assets/Modules/Vegetation/Shaders/LeafCardsPass.hlsl"
            ENDHLSL
  }
 }
}
