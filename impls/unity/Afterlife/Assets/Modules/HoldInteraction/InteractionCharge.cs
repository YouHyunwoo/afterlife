using System;

namespace Afterlife.Modules.HoldInteraction
{
    // 충전은 입력과 독립적이다. 실제 작업에 성공한 소비자만 충전량을 소비한다.
    public sealed class InteractionCharge
    {
        private double _elapsed;

        public float IntervalSeconds { get; }
        public float Fraction => (float)Math.Min(1d, _elapsed / IntervalSeconds);
        public float RemainingSeconds => (float)Math.Max(0d, IntervalSeconds - _elapsed);
        public bool Ready => _elapsed >= IntervalSeconds;

        public InteractionCharge(float intervalSeconds)
        {
            if (intervalSeconds <= 0 || float.IsNaN(intervalSeconds) || float.IsInfinity(intervalSeconds))
                throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            IntervalSeconds = intervalSeconds;
        }

        public void Advance(float deltaSeconds)
        {
            if (deltaSeconds <= 0 || float.IsNaN(deltaSeconds) || float.IsInfinity(deltaSeconds)) return;
            _elapsed = Math.Min(IntervalSeconds, _elapsed + deltaSeconds);
        }

        public bool TryConsume(bool held)
        {
            if (!held || !Ready) return false;
            _elapsed = 0;
            return true;
        }

        public void Reset(float fraction)
        {
            if (float.IsNaN(fraction) || float.IsInfinity(fraction) || fraction < 0 || fraction > 1)
                throw new ArgumentOutOfRangeException(nameof(fraction));
            _elapsed = IntervalSeconds * (double)fraction;
        }
    }
}
