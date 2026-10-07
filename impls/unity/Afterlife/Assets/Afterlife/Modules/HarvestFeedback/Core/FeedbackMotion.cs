using System;

namespace Afterlife.Modules.HarvestFeedback
{
    public readonly struct FeedbackPose
    {
        public readonly float X, Y, Scale, Alpha;
        public readonly bool Complete;
        public FeedbackPose(float x, float y, float scale, float alpha, bool complete)
        { X = x; Y = y; Scale = scale; Alpha = alpha; Complete = complete; }
    }

    // Canvas units in, Canvas units out. No camera, input or economy dependencies.
    public static class FeedbackMotion
    {
        private static float Clamp(float value) => Math.Max(0, Math.Min(1, value));
        public static FeedbackPose Text(float age, float lifetime, float rise)
        {
            float t = Clamp(age / Math.Max(.05f, lifetime)), entry = Clamp(t / .45f);
            float bounce = 1 - (float)(Math.Exp(-6 * entry) * Math.Cos(entry * Math.PI * 3));
            float alpha = 1 - Clamp((t - .48f) / .52f);
            return new FeedbackPose(0, rise * bounce + rise * .18f * t,
                .7f + .3f * bounce, alpha, t >= 1);
        }
        public static FeedbackPose Collection(float age, float burstSeconds, float flightSeconds,
            float startX, float startY, float scatterX, float scatterY, float targetX, float targetY, float arc)
        {
            float burst = Math.Max(.05f, burstSeconds), flight = Math.Max(.05f, flightSeconds);
            if (age < burst)
            {
                float t = Clamp(age / burst), ease = 1 - (float)Math.Pow(1 - t, 3);
                return new FeedbackPose(startX + scatterX * ease,
                    startY + scatterY * ease + (float)Math.Sin(t * Math.PI) * arc * .4f,
                    .65f + .45f * (float)Math.Sin(t * Math.PI * .5f), 1, false);
            }
            float progress = Clamp((age - burst) / flight), u = progress * progress, v = 1 - u;
            float x = startX + scatterX, y = startY + scatterY;
            float controlX = (x + targetX) * .5f, controlY = (y + targetY) * .5f + arc;
            return new FeedbackPose(v * v * x + 2 * v * u * controlX + u * u * targetX,
                v * v * y + 2 * v * u * controlY + u * u * targetY,
                1.1f - .85f * progress * progress, 1, progress >= 1);
        }
    }
}
