using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Data/Wave")]
    public sealed class PrototypeWaveData : ScriptableObject
    {
        public WaveRules Values = new WaveRules();
        public WaveRules Snapshot() => JsonUtility.FromJson<WaveRules>(JsonUtility.ToJson(Values));
    }
}
