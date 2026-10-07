using UnityEngine;
namespace Afterlife.Modules.Progression
{
    [CreateAssetMenu(menuName="Afterlife/Progression/Upgrade Tree")]
    public sealed class UpgradeTreeAsset : ScriptableObject
    {
        public UpgradeTreeData Data = new UpgradeTreeData();
        [HideInInspector] public bool EtheronCostsMigrated;
    }
}
