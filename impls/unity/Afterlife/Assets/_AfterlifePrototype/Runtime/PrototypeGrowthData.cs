using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Growth")]
    public sealed class PrototypeGrowthData : ScriptableObject
    {
        public GrowthRules Values = new GrowthRules();
        public GrowthRules Snapshot() => JsonUtility.FromJson<GrowthRules>(JsonUtility.ToJson(Values));
    }
}
