namespace Afterlife.Modules.Threshold
{
    /// <summary>Input-driven dialogue position, independent of Unity and presentation.</summary>
    public sealed class ThresholdIntroTimeline
    {
        public int Index { get; private set; }
        public bool Complete { get; private set; }
        public void Reset() { Index = 0; Complete = false; }
        public void Advance(int count)
        {
            if (Complete) return;
            Index++;
            Complete = Index >= count;
        }
    }
}
