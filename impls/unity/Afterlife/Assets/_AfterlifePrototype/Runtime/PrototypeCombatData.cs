using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Combat")]
    public sealed class PrototypeCombatData : ScriptableObject
    {
        public CombatRules Values = new CombatRules();
        public CombatRules Snapshot() => JsonUtility.FromJson<CombatRules>(JsonUtility.ToJson(Values));
    }
}
