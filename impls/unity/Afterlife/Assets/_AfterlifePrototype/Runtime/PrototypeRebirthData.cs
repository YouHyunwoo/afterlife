using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Rebirth")]
    public sealed class PrototypeRebirthData : ScriptableObject
    {
        public RebirthRules Values = new RebirthRules();
        public RebirthRules Snapshot() => JsonUtility.FromJson<RebirthRules>(JsonUtility.ToJson(Values));
    }
}
