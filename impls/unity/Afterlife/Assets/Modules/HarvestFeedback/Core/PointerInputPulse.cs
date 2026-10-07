using System;

namespace Afterlife.Modules.HarvestFeedback
{
    // Presentation state only. Input polling and game rules belong to the caller.
    public sealed class PointerInputPulse
    {
        public float PulseSeconds { get; set; } = .22f;
        public float Alpha { get; private set; }
        private float _remaining;

        public void Tick(float delta, bool clickMode, bool pressed, bool held)
        {
            _remaining = Math.Max(0, _remaining - Math.Max(0, delta));
            if (pressed) _remaining = Math.Max(.01f, PulseSeconds);
            Alpha = clickMode ? Math.Min(1, _remaining / Math.Max(.01f, PulseSeconds)) : (held ? 1 : 0);
        }

        public void Reset() { _remaining = 0; Alpha = 0; }
    }
}
