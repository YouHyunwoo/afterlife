using System;

namespace Afterlife.Devlog.Day02
{
    // Episode direction data. The shared Cinematics player owns the timeline.
    [Serializable]
    public sealed class Day02ShotPresentation
    {
        public bool Automatic, FixedTarget, Teaser;
        public int MaxWorks, TreeWorks;
        public float DemoStart;
        public string Explanation;
    }
}
