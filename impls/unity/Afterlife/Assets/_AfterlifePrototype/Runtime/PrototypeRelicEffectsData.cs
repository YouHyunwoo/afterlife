using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Relic")]
    public sealed class PrototypeRelicEffectsData : ScriptableObject
    {
        public RelicRules Values = new RelicRules();
        public RelicRules Snapshot() => JsonUtility.FromJson<RelicRules>(JsonUtility.ToJson(Values));
    }
}

