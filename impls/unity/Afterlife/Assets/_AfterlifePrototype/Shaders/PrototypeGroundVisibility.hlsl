#ifndef PROTOTYPE_GROUND_VISIBILITY
#define PROTOTYPE_GROUND_VISIBILITY
#include "Assets/Devlog/Youtube/Modules/Lighting/Shaders/ShowcaseVision.hlsl"
// 시야는 물체 표시 여부만 결정하고, 밤의 밝기는 건물 조명으로 결정한다.
half PrototypeGroundBrightness(float night,float illumination)
{
    return lerp(1,lerp(.08,1,saturate(illumination)),saturate(night));
}
#endif
