using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Loot")]
    public sealed class PrototypeLootData : ScriptableObject
    {
        public LootRules Values = new LootRules();
        public LootRules Snapshot() => JsonUtility.FromJson<LootRules>(JsonUtility.ToJson(Values));
    }
}
