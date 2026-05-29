using System;
using Afterlife.Domain.Progression.Effect.Data;

namespace Afterlife.Dev.UpgradeTree.Data
{
    [Serializable]
    public class UpgradeTreeLevelData
    {
        public int Cost;
        public string Description;
        public PlayerEffectParameter[] Parameters = Array.Empty<PlayerEffectParameter>();
    }
}
