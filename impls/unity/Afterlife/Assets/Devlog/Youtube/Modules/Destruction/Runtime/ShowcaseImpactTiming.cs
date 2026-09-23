using UnityEngine;

namespace Afterlife.Devlog.Destruction
{
    // 도구 접촉과 피격 반응이 공유하는 짧은 정지 구간. 전역 timeScale은 변경하지 않는다.
    public static class ShowcaseImpactTiming
    {
        public const float Hold = .045f;
        public static float Recoil(float age, bool wood)
        {
            float t = Mathf.Max(0, age-Hold);
            return Mathf.Exp(-t*(wood ? 12 : 19))*Mathf.Sin(t*(wood ? 29 : 39));
        }
        public static float Flash(float age) => .72f*(1-Mathf.SmoothStep(0,1,
            Mathf.Clamp01((age-Hold)/.075f)));
    }
}
