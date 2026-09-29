#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
float3 thresholdLight(Light light,float3 normal,float3 view,float power)
{
    float3 halfway=SafeNormalize(light.direction+view);
    float spec=pow(saturate(dot(normal,halfway)),power);
    spec=smoothstep(.08,.65,spec);
    return light.color*light.distanceAttenuation*light.shadowAttenuation*spec*saturate(dot(normal,light.direction));
}
float3 thresholdSpecular(float3 world,float3 normal,float power)
{
    float3 view=SafeNormalize(GetWorldSpaceViewDir(world));
    float3 result=thresholdLight(GetMainLight(),normal,view,power);
    #if defined(_ADDITIONAL_LIGHTS)
    uint count=GetAdditionalLightsCount();
    for(uint index=0;index<count;index++) result+=thresholdLight(GetAdditionalLight(index,world),normal,view,power);
    #endif
    return result;
}
