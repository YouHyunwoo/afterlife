using System;
namespace Afterlife.Modules.NightCleanup
{
    public sealed class NightCleanupClock
    {
        public float Elapsed { get; private set; }
        public bool Active { get; private set; }
        public bool ShowDirection => Active && Elapsed >= 20;
        public bool ShowBeacon => Active && (Elapsed >= 60 || Elapsed >= 40 && Elapsed % 2 < .65f);
        public void Tick(bool overtime, float seconds)
        {
            if (!overtime) { Reset(); return; }
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            Active = true;
            Elapsed = Math.Min(float.MaxValue, Elapsed + seconds);
        }
        public void Reset() { Active = false; Elapsed = 0; }
    }
}
