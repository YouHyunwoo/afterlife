using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Unit")]
    public sealed class PrototypeUnitData : ScriptableObject
    {
        public UnitRules Values = new UnitRules();
        public UnitRules Snapshot() => JsonUtility.FromJson<UnitRules>(JsonUtility.ToJson(Values));
    }
}
