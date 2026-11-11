using System;
using Afterlife.Buildings;

namespace Afterlife.Devlog.Day03
{
    public enum VillageTarget { None, Resource, Farm, SecondHouse }
    public enum VillageHarvestMode { Disabled, Filming, FreePlay }
    public interface IVillageGround
    {
        bool IsLand(int x,int z);
        bool HasResource(int x,int z);
    }

    // Filming configuration is independent of main-game balance.
    public sealed class VillageRules
    {
        public int StartingGold = 42, FarmCost = 30, HouseCost = 20, HarvestLimit = 3;
        public float FarmConstructionSeconds = 10, HouseConstructionSeconds = 10;
        public float ProductionSeconds = 3, WorkInterval = .7f, SupportSeconds = 1;
        public float HouseRadius = 2, HouseSpeedBonus = .1f;
        public int ProductionGold = 1;
        public int FarmWidth=1,FarmDepth=1,HouseWidth=1,HouseDepth=1;
        public void Validate()
        {
            if (StartingGold < 0 || FarmCost <= 0 || HouseCost <= 0 ||
                FarmConstructionSeconds <= 0 || HouseConstructionSeconds <= 0 ||
                ProductionSeconds <= 0 || WorkInterval <= 0 || SupportSeconds < 0 ||
                ProductionGold <= 0 || HarvestLimit < 0 || HouseRadius < 0 || HouseSpeedBonus < 0 ||
                FarmWidth<1 || FarmDepth<1 || HouseWidth<1 || HouseDepth<1)
                throw new ArgumentException("Invalid Day03 village rules.");
        }
    }

    public sealed class VillageSession
    {
        private readonly VillageRules _rules;
        private readonly IVillageGround _ground;
        private readonly BuildingFootprint _starterHouse;
        private readonly TownNetwork _town = new TownNetwork();
        private readonly ProductionCycle _production = new ProductionCycle();
        private double _workCooldown;
        public int Gold { get; private set; }
        public int FarmIncome { get; private set; }
        public int HarvestIncome { get; private set; }
        public bool FarmPurchased { get; private set; }
        public bool SecondHousePurchased { get; private set; }
        public float FarmProgress { get; private set; }
        public float HouseProgress { get; private set; }
        public BuildingFootprint FarmFootprint { get; private set; }
        public BuildingFootprint HouseFootprint { get; private set; }
        public System.Collections.Generic.IEnumerable<(int X,int Z)> Territory => _town.Tiles;
        public float ProductionProgress => _production.Progress;
        public float ProductionSeconds { get; private set; }
        public bool FarmComplete => FarmPurchased && FarmProgress >= 1;
        public bool SecondHouseComplete => SecondHousePurchased && HouseProgress >= 1;
        public event Action<int> GoldProduced;
        public event Action<VillageTarget> Worked;
        public VillageSession(VillageRules rules,IVillageGround ground)
        {
            rules.Validate(); _rules = rules;_ground=ground??throw new ArgumentNullException(nameof(ground)); Gold = rules.StartingGold;
            ProductionSeconds=rules.ProductionSeconds;
            _starterHouse=new BuildingFootprint(-1,0,rules.HouseWidth,rules.HouseDepth);
            RebuildTown();
        }
        // Independent filming snapshot; mutable economy and event subscriptions are not shared.
        public VillageSession Copy()
        {
            var copy=new VillageSession(_rules,_ground) {
                Gold=Gold,FarmIncome=FarmIncome,HarvestIncome=HarvestIncome,
                FarmPurchased=FarmPurchased,SecondHousePurchased=SecondHousePurchased,
                FarmProgress=FarmProgress,HouseProgress=HouseProgress,
                FarmFootprint=FarmFootprint,HouseFootprint=HouseFootprint,
                _workCooldown=_workCooldown,ProductionSeconds=ProductionSeconds
            };
            copy._production.SetProgress(_production.Progress);
            copy.RebuildTown();
            return copy;
        }
        // A cadence change starts a full cycle without changing earned income or construction.
        public void SetProductionSeconds(float seconds)
        {
            if(float.IsNaN(seconds)||float.IsInfinity(seconds)||seconds<=0)
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if(ProductionSeconds==seconds)return;
            ProductionSeconds=seconds;
            _production.SetProgress(0);
        }
        private void RebuildTown()
        {
            var first = new TownSource(_starterHouse, _rules.HouseRadius, _rules.HouseSpeedBonus);
            _town.Rebuild(SecondHouseComplete
                ? new[] { first, new TownSource(HouseFootprint, _rules.HouseRadius, _rules.HouseSpeedBonus) }
                : new[] { first });
        }
        public BuildingFootprint Footprint(VillageTarget target,int x,int z)=>target==VillageTarget.Farm
            ?new BuildingFootprint(x,z,_rules.FarmWidth,_rules.FarmDepth)
            :new BuildingFootprint(x,z,_rules.HouseWidth,_rules.HouseDepth);
        public string PlacementError(VillageTarget target,int x,int z)
        {
            if(target!=VillageTarget.Farm&&target!=VillageTarget.SecondHouse)return "건물을 선택하세요.";
            if(target==VillageTarget.Farm?FarmPurchased:SecondHousePurchased)return "이번 촬영의 해당 건물은 이미 배치했습니다.";
            var area=Footprint(target,x,z);
            foreach(var cell in Cells(area)) {
                if(!_ground.IsLand(cell.X,cell.Z))return "물 타일에는 건설할 수 없습니다.";
                if(target!=VillageTarget.SecondHouse&&!InTerritory(cell.X,cell.Z))return "모든 점유 타일이 마을 영역 안이어야 합니다.";
                if(_starterHouse.Contains(cell.X,cell.Z)||FarmPurchased&&FarmFootprint.Contains(cell.X,cell.Z)||
                    SecondHousePurchased&&HouseFootprint.Contains(cell.X,cell.Z)||_ground.HasResource(cell.X,cell.Z))
                    return "다른 오브젝트와 겹칩니다.";
            }
            return Gold<(target==VillageTarget.Farm?_rules.FarmCost:_rules.HouseCost)?"골드가 부족합니다.":null;
        }
        public bool InTerritory(int x,int z)
        {
            foreach(var cell in _town.Tiles)if(cell.X==x&&cell.Z==z)return true;
            return false;
        }
        private static System.Collections.Generic.IEnumerable<(int X,int Z)> Cells(BuildingFootprint area)
        {
            for(int x=area.X;x<=area.Right;x++)for(int z=area.Z;z<=area.Top;z++)yield return (x,z);
        }
        public bool Place(VillageTarget target,int x,int z)
        {
            if(PlacementError(target,x,z)!=null)return false;
            if(target==VillageTarget.Farm){Gold-=_rules.FarmCost;FarmPurchased=true;FarmFootprint=Footprint(target,x,z);}
            else {Gold-=_rules.HouseCost;SecondHousePurchased=true;HouseFootprint=Footprint(target,x,z);}
            return true;
        }
        public void Tick(float elapsed, VillageTarget target, bool held, bool allowHarvest)
            =>Tick(elapsed,target,held,allowHarvest?VillageHarvestMode.Filming:VillageHarvestMode.Disabled);

        public void Tick(float elapsed, VillageTarget target, bool held, VillageHarvestMode harvestMode)
        {
            if (float.IsNaN(elapsed) || float.IsInfinity(elapsed) || elapsed < 0)
                throw new ArgumentOutOfRangeException(nameof(elapsed));
            // Fixed small slices preserve construction/production boundary timing on long frames.
            double remaining = elapsed;
            while (remaining > .0000001)
            {
                float step = (float)Math.Min(.02, remaining); remaining -= step;
                bool farmWasComplete = FarmComplete;
                _workCooldown = Math.Max(0, _workCooldown - step);
                if (FarmPurchased && !FarmComplete)
                    FarmProgress = Math.Min(1, FarmProgress + step * _town.ConstructionMultiplier(FarmFootprint) / _rules.FarmConstructionSeconds);
                if (SecondHousePurchased && !SecondHouseComplete)
                {
                    HouseProgress = Math.Min(1, HouseProgress + step * _town.ConstructionMultiplier(HouseFootprint) / _rules.HouseConstructionSeconds);
                    if (SecondHouseComplete) RebuildTown();
                }
                if (held && _workCooldown <= .0000001 && CanWork(target, harvestMode))
                {
                    _workCooldown = _rules.WorkInterval;
                    if (target == VillageTarget.Resource) { Gold++; HarvestIncome++; }
                    if (target == VillageTarget.Farm) FarmProgress = Math.Min(1, FarmProgress + _rules.SupportSeconds / _rules.FarmConstructionSeconds);
                    if (target == VillageTarget.SecondHouse)
                    {
                        HouseProgress = Math.Min(1, HouseProgress + _rules.SupportSeconds / _rules.HouseConstructionSeconds);
                        if (SecondHouseComplete) RebuildTown();
                    }
                    Worked?.Invoke(target);
                }
                if (farmWasComplete && _production.Advance(step, ProductionSeconds))
                {
                    _production.Complete(); Gold += _rules.ProductionGold; FarmIncome += _rules.ProductionGold;
                    GoldProduced?.Invoke(_rules.ProductionGold);
                }
            }
        }
        private bool CanWork(VillageTarget target, VillageHarvestMode harvestMode) =>
            target == VillageTarget.Resource ? harvestMode==VillageHarvestMode.FreePlay ||
                harvestMode==VillageHarvestMode.Filming && !FarmPurchased && HarvestIncome < _rules.HarvestLimit :
            target == VillageTarget.Farm ? FarmPurchased && !FarmComplete :
            target == VillageTarget.SecondHouse && SecondHousePurchased && !SecondHouseComplete;
    }
}
