Shader "Afterlife/UI/ModalBackdropBlur"
{
    Properties { _MainTex ("Source", 2D) = "white" {} _Darkness ("Darkness", Range(0,1)) = 0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Direction;
            float _Darkness;
            float _FlipY;
            fixed4 frag(v2f_img i) : SV_Target
            {
                float2 stepUV=_Direction.xy*.25;
                float2 uv=i.uv;
                uv.y=lerp(uv.y,1-uv.y,_FlipY);
                half3 color=tex2D(_MainTex,uv).rgb*.227027;
                color+=(tex2D(_MainTex,uv+stepUV).rgb+tex2D(_MainTex,uv-stepUV).rgb)*.1945946;
                color+=(tex2D(_MainTex,uv+stepUV*2).rgb+tex2D(_MainTex,uv-stepUV*2).rgb)*.1216216;
                color+=(tex2D(_MainTex,uv+stepUV*3).rgb+tex2D(_MainTex,uv-stepUV*3).rgb)*.054054;
                color+=(tex2D(_MainTex,uv+stepUV*4).rgb+tex2D(_MainTex,uv-stepUV*4).rgb)*.016216;
                return half4(color*(1-_Darkness),1);
            }
            ENDHLSL
        }
    }
}
