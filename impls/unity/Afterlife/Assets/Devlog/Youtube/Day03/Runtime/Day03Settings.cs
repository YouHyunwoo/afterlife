using UnityEngine;
namespace Afterlife.Devlog.Day03
{
    public sealed class Day03Settings : ScriptableObject
    {
        public int StartingGold = 42, FarmCost = 30, HouseCost = 20, ProductionGold = 1, HarvestLimit = 3;
        public float FarmConstructionSeconds = 10, HouseConstructionSeconds = 10, ProductionSeconds = 3;
        public float WorkInterval = .7f, SupportSeconds = 1, HouseRadius = 2, HouseSpeedBonus = .1f;
        public int FarmWidth=1,FarmDepth=1,HouseWidth=1,HouseDepth=1;
        public VillageRules CreateRules() => new VillageRules {
            StartingGold=StartingGold, FarmCost=FarmCost, HouseCost=HouseCost, ProductionGold=ProductionGold,
            FarmConstructionSeconds=FarmConstructionSeconds, HouseConstructionSeconds=HouseConstructionSeconds,
            ProductionSeconds=ProductionSeconds, WorkInterval=WorkInterval, SupportSeconds=SupportSeconds,
            HouseRadius=HouseRadius, HouseSpeedBonus=HouseSpeedBonus, HarvestLimit=HarvestLimit,
            FarmWidth=FarmWidth,FarmDepth=FarmDepth,HouseWidth=HouseWidth,HouseDepth=HouseDepth
        };
    }
}
