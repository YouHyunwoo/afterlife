Shader "Afterlife/NightCleanup/Marker"
{
 Properties { _Color ("Color", Color) = (1, .7, .2, 1) }
 SubShader
 {
  Tags { "Queue"="Overlay" "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" }
  Pass
  {
   ZTest Always ZWrite Off Cull Off Blend SrcAlpha OneMinusSrcAlpha
   HLSLPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
   struct Attributes { float4 positionOS : POSITION; };
   struct Varyings { float4 positionCS : SV_POSITION; };
   CBUFFER_START(UnityPerMaterial)
   float4 _Color;
   CBUFFER_END
   Varyings vert(Attributes input) { Varyings output; output.positionCS=TransformObjectToHClip(input.positionOS.xyz); return output; }
   half4 frag(Varyings input) : SV_Target { return _Color; }
   ENDHLSL
  }
 }
}
