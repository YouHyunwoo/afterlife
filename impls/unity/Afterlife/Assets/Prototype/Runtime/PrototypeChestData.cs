using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Chest")]
    public sealed class PrototypeChestData : ScriptableObject
    {
        public ChestRules Values = new ChestRules();
        public ChestRules Snapshot() => JsonUtility.FromJson<ChestRules>(JsonUtility.ToJson(Values));
    }
}
