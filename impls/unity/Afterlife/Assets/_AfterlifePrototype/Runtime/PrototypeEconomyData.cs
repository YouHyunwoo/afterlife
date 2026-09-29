using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Economy")]
    public sealed class PrototypeEconomyData : ScriptableObject
    {
        public EconomyRules Values = new EconomyRules();
        public EconomyRules Snapshot() => JsonUtility.FromJson<EconomyRules>(JsonUtility.ToJson(Values));
    }
}
