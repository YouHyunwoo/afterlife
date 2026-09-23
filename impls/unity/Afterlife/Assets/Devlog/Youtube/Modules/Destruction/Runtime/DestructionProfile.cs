using UnityEngine;

namespace Afterlife.Devlog.Destruction
{
    [CreateAssetMenu(menuName="Devlog/Destruction Profile",fileName="DestructionProfile")]
    public sealed class DestructionProfile : ScriptableObject
    {
        [Min(1)] public int HitsToBreak=4;
        [Min(.1f)] public float TreeFallSeconds=1.1f;
        [Range(0,90)] public float TreeFallDegrees=88;
        [Min(0)] public float TreeHoldSeconds=.55f;
        [Range(2,12)] public int RockFragmentCount=6;
        [Min(.1f)] public float RockRollSeconds=1.5f;
        [Min(.1f)] public float RemovalSeconds=1.15f;
    }
}
