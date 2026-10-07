Shader "Afterlife/Threshold/Veil"
{
    Properties { _MainTex("World",2D)="black"{} _Cover("Cover",Range(0,1))=0 _Modal("Modal",Range(0,1))=0 _Result("Result",Range(0,1))=0 _Blur("Blur",Range(0,1))=0 _Darkness("Darkness",Range(0,1))=0 _Clock("Clock",Float)=0 _Motion("Motion",Float)=1 _Aspect("Aspect",Float)=1.777 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Cull Off ZWrite Off ZTest Always
        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex; float4 _MainTex_TexelSize;
            float _Cover,_Modal,_Result,_Blur,_Darkness,_Clock,_Motion,_Aspect;
            fixed4 frag(v2f_img i):SV_Target
            {
                float2 uv=i.uv; float time=_Clock*_Motion;
                float2 warp=float2(sin(uv.y*16+time*.8),sin(uv.x*19-time*.6))*.007*_Blur*_Motion;
                float2 p=clamp(uv+warp,.002,.998);
                // Adjacent texels, not distant copies: separable binomial weights in a small 5x5 footprint.
                fixed3 filtered=0;
                [unroll] for(int y=-2;y<=2;y++)
                {
                    float wy=y==0?6:(abs(y)==1?4:1);
                    [unroll] for(int x=-2;x<=2;x++)
                    {
                        float wx=x==0?6:(abs(x)==1?4:1);
                        filtered+=tex2D(_MainTex,clamp(p+float2(x,y)*_MainTex_TexelSize.xy,.002,.998)).rgb*(wx*wy/256);
                    }
                }
                fixed3 c=lerp(tex2D(_MainTex,p).rgb,filtered,saturate(_Blur));
                float2 q=(uv-.5)*2; q.x*=_Aspect;
                float radius=length(q)/length(float2(_Aspect,1));
                float noise=(sin(uv.x*14+time*.21)*sin(uv.y*11-time*.17)+sin(uv.x*29-uv.y*17+time*.29)*.35)*.035;
                float threshold=lerp(1.15,-.18,_Cover);
                float mask=smoothstep(threshold-.12,threshold+.09,radius+noise);
                float edge=1-smoothstep(0,.24,min(min(uv.x,1-uv.x),min(uv.y,1-uv.y))+noise);
                float dim=max(max(_Modal*.48,_Result*.55),_Darkness);
                c*=1-dim;
                float front=lerp(-.08,.25,_Result);
                float resultEdge=1-smoothstep(front-.07,front+.07,min(min(uv.x,1-uv.x),min(uv.y,1-uv.y))+noise);
                c=lerp(c,fixed3(0,0,0),saturate(mask+edge*_Modal*.92+resultEdge*_Result*.96));
                return fixed4(c,1);
            }
            ENDCG
        }
    }
}
