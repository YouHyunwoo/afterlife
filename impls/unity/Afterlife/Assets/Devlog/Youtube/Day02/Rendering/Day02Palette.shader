Shader "Afterlife/Devlog/Day02Palette"
{
 Properties { _BaseColor("Color",Color)=(1,1,1,1) _VertexColors("Vertex Colors",Float)=1 }
 SubShader {
 Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
 Pass {
 HLSLPROGRAM
 #pragma vertex Vert
 #pragma fragment Frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseColor; float _VertexColors;
 CBUFFER_END
 struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float4 color:COLOR; };
 struct V { float4 positionCS:SV_POSITION; float3 normalWS:TEXCOORD0; float4 color:COLOR; };
 V Vert(A v) { V o;o.positionCS=TransformObjectToHClip(v.positionOS.xyz);o.normalWS=TransformObjectToWorldNormal(v.normalOS);o.color=lerp(float4(1,1,1,1),v.color,_VertexColors)*_BaseColor;return o; }
 half4 Frag(V v):SV_Target { Light light=GetMainLight();float diffuse=saturate(dot(normalize(v.normalWS),light.direction));return half4(v.color.rgb*(.48+diffuse*.52)*lerp(half3(1,1,1),light.color,.25),1); }
 ENDHLSL
 }
 }
}
