namespace Afterlife.Devlog.Day02
{
    public readonly struct HarvestDemoInput
    {
        public readonly bool Stone, Held, Pressed;
        public HarvestDemoInput(bool stone, bool held, bool pressed)
        { Stone=stone; Held=held; Pressed=pressed; }
    }

    // Filming direction follows accepted work, so a busy tool never skips a target.
    public static class HarvestDemoSequence
    {
        public static HarvestDemoInput Evaluate(int workCount, int treeWorks, int maxWorks,
            bool active, bool clickMode, bool ready)
        {
            bool held=active&&(maxWorks<=0||workCount<maxWorks);
            return new HarvestDemoInput(workCount>=treeWorks,held,held&&clickMode&&ready);
        }
    }
}
