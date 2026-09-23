#ifndef PROTOTYPE_GROUND_VISIBILITY
#define PROTOTYPE_GROUND_VISIBILITY
#include "Assets/Devlog/Youtube/Modules/Lighting/Shaders/ShowcaseVision.hlsl"
// 지형과 식생이 같은 시야 계수로 낮/밤 밝기를 감쇠한다.
half PrototypeGroundBrightness(float night,float visibility)
{
    return ShowcaseVisionBrightness(night,visibility);
}
#endif
