using System;
namespace Afterlife.Buildings
{
    public sealed class ProductionCycle
    {
        private double _progress;
        public float Progress => (float)Math.Min(1,_progress);
        public bool Ready => _progress>=1-0.0000001;
        public void SetProgress(float progress) => _progress=Math.Max(0,Math.Min(1,progress));
        public float Remaining(float seconds) => (float)Math.Max(0,1-_progress)*Math.Max(.01f,seconds);
        public bool Advance(float elapsed,float seconds)
        { _progress+=Math.Max(0,elapsed)/Math.Max(.01f,seconds);return Ready; }
        public void Complete() => _progress=Math.Max(0,_progress-1);
    }
}
