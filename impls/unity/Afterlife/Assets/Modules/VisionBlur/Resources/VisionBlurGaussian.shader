Shader "Afterlife/VisionBlurGaussian"
{
    Properties { _BlurSource ("Frame", 2D) = "white" {} }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always
        CGINCLUDE
        #include "UnityCG.cginc"
        Texture2D _BlurSource;
        // 원본의 Point 필터와 무관하게 블러에서는 선형 보간을 사용한다.
        SamplerState sampler_LinearClamp;
        float4 _BlurSource_TexelSize;

        float4 Gaussian(float2 uv, float2 axis)
        {
            float scale = clamp(_BlurSource_TexelSize.w / 150.0, 1.0, 8.0);
            float sigma = 1.6 * scale;
            int radius = (int)ceil(4.0 * scale);
            float4 color = 0;
            float totalWeight = 0;
            // 샘플 사이를 건너뛰지 않고 연속된 텍셀 전체를 평균한다.
            [loop] for (int offset = -radius; offset <= radius; offset++)
            {
                float weight = exp(-0.5 * offset * offset / (sigma * sigma));
                float2 sampleUV = uv + axis * _BlurSource_TexelSize.xy * offset;
                color += _BlurSource.SampleLevel(sampler_LinearClamp, sampleUV, 0) * weight;
                totalWeight += weight;
            }
            return color / totalWeight;
        }
        ENDCG
        Pass
        {
            Name "Horizontal"
            Blend Off
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment FragHorizontal
            float4 FragHorizontal(v2f_img input) : SV_Target
            {
                return Gaussian(input.uv, float2(1, 0));
            }
            ENDCG
        }

        Pass
        {
            Name "Vertical"
            Blend Off
            CGPROGRAM
            #pragma target 3.5
            #pragma vertex vert_img
            #pragma fragment FragVertical
            float4 FragVertical(v2f_img input) : SV_Target
            {
                return Gaussian(input.uv, float2(0, 1));
            }
            ENDCG
        }
    }
}
