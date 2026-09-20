#ifndef DEVLOG_DAMAGE_MARKS_INCLUDED
#define DEVLOG_DAMAGE_MARKS_INCLUDED
float DamageGrain(float2 p)
{
    return frac(sin(dot(floor(p*95),float2(127.1,311.7)))*43758.5453);
}
half3 DamageSurface(half3 color,float3 local)
{
    // 합쳐진 수관의 초록 정점색에는 도끼 자국을 그리지 않는다.
    if (_DamageWood>.5 && color.g>color.r*1.08 && color.g>color.b*1.05) return color;
    [loop] for (int k=0;k<_DamageMarkCountV1;k++)
    {
        float3 delta=local-_DamageMarksV1[k].xyz;
        float3 normal=_DamageNormalsV1[k].xyz;
        float depth=abs(dot(delta,normal));
        if (depth>.075) continue;
        float3 side=normalize(cross(abs(normal.y)>.9 ? float3(0,0,1) : float3(0,1,0),normal));
        float3 vertical=cross(normal,side);
        float2 q=float2(dot(delta,side),dot(delta,vertical));
        float strength=_DamageMarksV1[k].w;
        float noise=DamageGrain(q);
        float planeFade=1-smoothstep(.025,.075,depth);
        float shape;
        half3 inside;
        if (_DamageWood>.5)
        {
            // 비스듬하고 얕은 V형 껍질 자국. 균일한 원형 도장은 사용하지 않는다.
            q.y+=q.x*.23;
            float width=.085+.105*strength;
            float groove=abs(q.y+abs(q.x)*.17)/(.018+.028*strength);
            shape=max(abs(q.x)/width,groove)+noise*.24;
            inside=half3(.57,.34,.13)*(.90+.10*noise);
        }
        else
        {
            float radius=.075+.14*strength;
            shape=max(abs(q.x)*.94+abs(q.y)*.42,abs(q.y)*.95+abs(q.x)*.29)/radius+noise*.23;
            inside=min(half3(.72,.77,.78),color*1.20+half3(.035,.04,.04));
            // 자국에서 짧게 뻗는 세 갈래 균열. 진한 선은 반복 타격 뒤에만 드러난다.
            float crack=0;
            [unroll] for (int branchIndex=0;branchIndex<3;branchIndex++)
            {
                float angle=branchIndex*2.19+.37;
                float2 axis=float2(cos(angle),sin(angle));
                float along=dot(q,axis), across=dot(q,float2(-axis.y,axis.x));
                float bend=sin(along*71+branchIndex)*.007;
                float thin=1-smoothstep(.002,.006,abs(across-bend));
                crack=max(crack,thin*smoothstep(radius*.5,radius*.8,along)*(1-smoothstep(radius,radius*(1+strength),along)));
            }
            color=lerp(color,color*.38,crack*planeFade*saturate((strength-.34)*1.5));
        }
        float edge=(1-smoothstep(.95,1.10,shape))*planeFade;
        float core=(1-smoothstep(.72,.96,shape))*planeFade;
        color=lerp(color,color*.55,edge*.65);
        color=lerp(color,inside,core);
    }
    return color;
}
#endif
