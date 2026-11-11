using System;

namespace Afterlife.Modules.AmbientAudio
{
    // 씬/오디오 API와 무관한 페이드와 간헐음 스케줄. 소유자가 재생 가능 여부를 주입한다.
    public sealed class AmbientTimeline
    {
        private readonly Random _random;
        private float _remaining;
        private bool _birdsAllowed;
        public float Blend { get; private set; }
        public float BirdBlend { get; private set; }
        public event Action BirdRequested;

        public AmbientTimeline(Random random) { _random = random ?? throw new ArgumentNullException(nameof(random)); Reset(); }
        public void Reset()
        {
            Blend = BirdBlend = 0;
            _birdsAllowed = false;
            _remaining = Range(3, 8);
        }
        public void Tick(float delta, bool active, bool birdsAllowed, bool birdBusy, float minimumGap, float maximumGap)
        {
            delta = Math.Max(0, delta);
            Blend = Move(Blend, active ? 1 : 0, delta / 2);
            bool allowed = active && birdsAllowed;
            BirdBlend = Move(BirdBlend, allowed ? 1 : 0, delta);
            if (allowed && !_birdsAllowed) _remaining = Range(3, 8);
            _birdsAllowed = allowed;
            if (!allowed || birdBusy) return;
            _remaining -= delta;
            if (_remaining > 0) return;
            minimumGap = Math.Max(1, minimumGap);
            _remaining = Range(minimumGap, Math.Max(minimumGap, maximumGap));
            BirdRequested?.Invoke();
        }
        private float Range(float min, float max) => min + (max - min) * (float)_random.NextDouble();
        private static float Move(float value, float target, float delta) => value < target
            ? Math.Min(target, value + delta) : Math.Max(target, value - delta);
    }
}
