float thresholdHash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
float thresholdNoise(float2 p){float2 a=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(thresholdHash(a),thresholdHash(a+float2(1,0)),f.x),lerp(thresholdHash(a+float2(0,1)),thresholdHash(a+1),f.x),f.y);}
float portalEdge(float2 uv,float t,float motion)
{
    float2 p=uv*2-1;
    // atan2 has no defined direction at the origin. Supply a finite direction before evaluation.
    float2 angularInput=dot(p,p)<.000001 ? float2(1,0) : p;
    float angle=atan2(angularInput.y,angularInput.x);
    float jagged=.76+.065*(abs(frac(angle/6.283185*5+.15)*2-1)*2-1)
        +.04*(abs(frac(angle/6.283185*9)*2-1)*2-1)+.018*sin(angle*17);
    float ink=thresholdNoise(p*19+float2(t*.055,-t*.04))*.045+thresholdNoise(p*47)*.022;
    float crawl=(thresholdNoise(p*4.5+float2(t*.18,-t*.13))-.5)*.12+sin(angle*6+t*.65)*.018*motion;
    return length(p)-jagged+ink+crawl;
}
float portalHeight(float2 uv,float t,float motion)
{
    float2 p=uv*2-1;
    // Only the outer membrane deforms. Keep all central vertices and their normals planar.
    if(dot(p,p)<.1225) return 0;
    float edge=portalEdge(uv,t,motion);
    float offset=(edge+.04)/.13;
    // HLSL pow with a negative base can produce NaN even with exponent 2.
    float band=exp(-(offset*offset))*smoothstep(.35,.5,length(p));
    return band*(.035+.07*sin(p.x*11+p.y*7+t*.85)+.025*cos(p.y*19-p.x*9-t*.6));
}
