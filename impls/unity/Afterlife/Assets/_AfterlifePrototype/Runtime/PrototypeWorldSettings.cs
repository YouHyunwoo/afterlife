using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Settings/World")]
    public sealed class PrototypeWorldSettings : ScriptableObject
    {
        public float ResourceSpawnSeconds = 12;
        public int TreeLimit = 780;
        public int RockLimit = 480;
        public int MapSeed = 0;
        public int MapRadius = 45;
        [Header("Economic sites")]
        [Min(0)] public int FertileSiteCount = 2;
        [Min(0)] public int LeylineSiteCount = 2;
        [Min(1)] public int FertileSiteSize = 3;
        [Min(1)] public int LeylineSiteSize = 3;
        [Min(0)] public int EconomicSiteMinimumDistance = 8;
    }
}
