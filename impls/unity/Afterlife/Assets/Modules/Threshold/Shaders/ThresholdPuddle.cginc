// Shared local-space silhouette: ripples and portal reflections share one shoreline.
float4x4 _PuddleWorldToLocal;
float _UsePuddleTransform;
float2 puddlePoint(float3 world) { return lerp(world, mul(_PuddleWorldToLocal,float4(world,1)).xyz,_UsePuddleTransform).xz; }
float puddleDistance(float2 p)
{
    float2 q=(p-float2(0,-2))/float2(2.5,5);
    float a=atan2(q.y,q.x);
    float boundary=.88+.065*sin(a*3+.8)+.035*sin(a*7-1.2)+.02*cos(a*11);
    return length(q)-boundary;
}
