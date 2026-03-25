using System.Collections.Generic;

namespace Afterlife.Dev.Field
{
    public class HoldableVisible : Moonstone.Ore.Local.Visible
    {
        private readonly Dictionary<string, float> _holdings = new();

        public Dictionary<string, float> Holdings => _holdings;
    }
}