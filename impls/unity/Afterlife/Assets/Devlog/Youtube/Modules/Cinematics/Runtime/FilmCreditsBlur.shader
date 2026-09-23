Shader "Afterlife/Devlog/FilmCreditsBlur"
{
    Properties { _MainTex("Source",2D)="white"{} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            float4 _Direction;
            float _Sigma;
            half4 frag(v2f_img i):SV_Target
            {
                // 강도에 따라 간격을 벌리지 않고 모든 인접 픽셀을 누적해 복제 윤곽을 막는다.
                float sigma=clamp(_Sigma,0.05,16.0);
                int radius=(int)ceil(3.0*sigma);
                float4 sum=0;
                float total=0;
                [loop] for(int tap=-radius;tap<=radius;tap++)
                {
                    float weight=exp(-0.5*(float)(tap*tap)/(sigma*sigma));
                    sum+=tex2D(_MainTex,saturate(i.uv+_Direction.xy*tap))*weight;
                    total+=weight;
                }
                return half4(sum.rgb/total,1);
            }
            ENDCG
        }
    }
}
