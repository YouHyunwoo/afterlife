Shader "Afterlife/VisionBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Frame", 2D) = "white" {}
        _BlurredTex ("Blurred frame", 2D) = "black" {}
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Cull Off ZWrite Off ZTest Always

        CGINCLUDE
        #include "UnityCG.cginc"
        sampler2D _MainTex, _BlurredTex;
        float4 _CursorCenter, _CursorXAxis, _CursorZAxis;
        float _HasCursor;
        ENDCG

        // 원본과 완성된 가우시안 블러를 마우스의 XZ 시야 거리로 혼합한다.
        Pass
        {
            Name "Composite"
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            struct Attributes { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct Varyings { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            float VisionDistance(float2 uv)
            {
                float2 delta = uv - _CursorCenter.xy;
                float2 xAxis = _CursorXAxis.xy, zAxis = _CursorZAxis.xy;
                float determinant = xAxis.x * zAxis.y - xAxis.y * zAxis.x;
                determinant = abs(determinant) < .000001 ? (determinant < 0 ? -.000001 : .000001) : determinant;
                float2 ground = float2(delta.x * zAxis.y - delta.y * zAxis.x,
                    xAxis.x * delta.y - xAxis.y * delta.x) / determinant;
                return length(ground);
            }

            float4 Frag(Varyings input) : SV_Target
            {
                float distanceFromCursor = _HasCursor > .5 ? VisionDistance(input.uv) : 1000;
                float blur = smoothstep(1, 3, distanceFromCursor);
                float4 original = tex2D(_MainTex, input.uv);
                float4 blurred = tex2D(_BlurredTex, input.uv);
                return float4(lerp(original.rgb, blurred.rgb, blur), original.a) * input.color;
            }
            ENDCG
        }

    }
}
