using Afterlife.Domain.Progression.Effect.Data;
using UnityEngine;

namespace Afterlife.Domain.Progression.Upgrade.Data
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "Afterlife/Data/Upgrade")]
    public class UpgradeData : ScriptableObject
    {
        public string Id;
        public Sprite Icon;
        public int Cost;
        public PlayerEffectParameter[] Parameters;
    }
}
