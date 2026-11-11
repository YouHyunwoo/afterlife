using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Resource")]
    public sealed class PrototypeResourceData : ScriptableObject
    {
        public ResourceRules Values = new ResourceRules();
        public ResourceRules Snapshot() => JsonUtility.FromJson<ResourceRules>(JsonUtility.ToJson(Values));
    }
}
