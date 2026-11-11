using System;
using System.Collections.Generic;
using System.Linq;

namespace Afterlife.Prototype
{
    // Unity 참조가 없는 판별 스냅샷. SO는 조립 계층에서 이 값들을 깊은 복사한다.
    [Serializable] public sealed class BuildingStats
    {
        public float Health = 120, ConstructionSeconds = 10;
        public int Width = 1, Depth = 1, PopulationCapacity;
        public float VisionRadius = 1, TerritoryRadius;
        public float LightRadius;
        public float ConstructionSpeedBonus;
        public bool BlocksMovement = true, DirectBuild = true, CanMerge;
        public Kind MergeTarget, ProducedUnit;
        public ProductionKind Production;
        public float ProductionSeconds;
        public int ProductionAmount = 1, TroopCapacity = 3, AccelerationGoldCost;
        public float AttackDamage, AttackRange, AttackSeconds, ProjectileSpeed = 12;
        public static BuildingStats Defaults(Kind id)
        {
            bool merged=id==Kind.TownHall||id==Kind.Sanctuary;
            bool troop=id==Kind.Barracks||id==Kind.ArcheryRange||id==Kind.KnightHall||id==Kind.MageTower;
            return new BuildingStats
            {
                Health=id==Kind.House?180:id==Kind.TownHall?720:id==Kind.Sanctuary?480:id==Kind.Wall?260:id==Kind.ArrowTower||id==Kind.MagicTower?190:120,
                Width=merged||id==Kind.LargeTest?2:1, Depth=merged?2:1, ConstructionSeconds=id==Kind.Barracks?15:10,
                TerritoryRadius=id==Kind.House?2:id==Kind.TownHall?4:0, VisionRadius=id==Kind.House?2:id==Kind.TownHall?4:1,
                LightRadius=id==Kind.House?1.95f:id==Kind.TownHall?2f:id==Kind.Barracks||id==Kind.Market||id==Kind.LargeTest?.5f:0,
                ConstructionSpeedBonus=id==Kind.House?.1f:id==Kind.TownHall?.6f:0,
                BlocksMovement=id!=Kind.Farm, DirectBuild=!merged&&id!=Kind.Research&&id!=Kind.Market,
                CanMerge=id==Kind.House||id==Kind.Altar, MergeTarget=id==Kind.House?Kind.TownHall:Kind.Sanctuary,
                Production=troop?ProductionKind.Troop:id==Kind.Farm?ProductionKind.Gold:id==Kind.Altar||id==Kind.Sanctuary?ProductionKind.Etheron:ProductionKind.None,
                ProducedUnit=id==Kind.ArcheryRange?Kind.Archer:id==Kind.KnightHall?Kind.Knight:id==Kind.MageTower?Kind.Mage:Kind.Warrior,
                ProductionSeconds=id==Kind.Farm?3:id==Kind.Altar?30:id==Kind.Sanctuary?20:id==Kind.KnightHall?18:id==Kind.MageTower?15:troop?10:0,
                ProductionAmount=id==Kind.Sanctuary?4:1, TroopCapacity=3,
                AccelerationGoldCost=troop?5:id==Kind.Altar?50:id==Kind.Sanctuary?40:0,
                AttackDamage=id==Kind.MagicTower?34:18, AttackRange=id==Kind.MagicTower?7:6, AttackSeconds=id==Kind.MagicTower?2.8f:1.6f
            };
        }
    }
    [Serializable] public sealed class UnitDefinition
    {
        public Kind Id;
        public string DisplayName;
        public float Health = 60, Damage = 5, Range = .52f, MoveSpeed = 2, AttackSeconds = .9f, Vision = 8;
        public float RecruitmentMultiplier = 1, ProjectileSpeed = 11;
        public int Price, KillGold;
        public bool RequiresBuilding;
        public Kind RequiredBuilding;
        public LootKind[] Loot = Array.Empty<LootKind>();
    }
    [Serializable] public sealed class UnitRules
    {
        public List<UnitDefinition> Entries = Defaults();
        public UnitDefinition Find(Kind id) => Entries.Find(x => x.Id == id) ?? throw new ArgumentException("유닛 데이터 누락: " + id);
        private static List<UnitDefinition> Defaults() => new List<UnitDefinition>
        {
            new UnitDefinition { Id=Kind.Warrior, DisplayName="전사", Health=90, Damage=12, Price=10 },
            new UnitDefinition { Id=Kind.Archer, DisplayName="궁수", Damage=11, Range=4.5f, Price=15, AttackSeconds=1.5f },
            new UnitDefinition { Id=Kind.Mage, DisplayName="마법사", Damage=20, Range=4, Price=30, RecruitmentMultiplier=1.5f, RequiresBuilding=true, RequiredBuilding=Kind.Altar, AttackSeconds=1.5f, ProjectileSpeed=7 },
            new UnitDefinition { Id=Kind.Knight, DisplayName="기사", Health=150, Damage=18, Price=40, RecruitmentMultiplier=1.8f, RequiresBuilding=true, RequiredBuilding=Kind.Altar },
            new UnitDefinition { Id=Kind.Slime, DisplayName="슬라임", Health=30, MoveSpeed=1.2f, AttackSeconds=1.2f, KillGold=2, Loot=new[]{LootKind.SlimeGel} },
            new UnitDefinition { Id=Kind.Goblin, DisplayName="고블린", Health=36, Damage=7, MoveSpeed=2.15f, KillGold=3, Loot=new[]{LootKind.Club,LootKind.Bone} },
            new UnitDefinition { Id=Kind.GoblinWarrior, DisplayName="고블린 전사", Health=80, Damage=10, MoveSpeed=1.45f, KillGold=5, Loot=new[]{LootKind.Armor} },
            new UnitDefinition { Id=Kind.GoblinShaman, DisplayName="고블린 주술사", Health=55, Damage=14, Range=4.8f, MoveSpeed=1.3f, AttackSeconds=1.5f, Vision=10, ProjectileSpeed=7, KillGold=4, Loot=new[]{LootKind.Staff} },
            new UnitDefinition { Id=Kind.Orc, DisplayName="오크", Health=140, Damage=18, Range=.65f, MoveSpeed=.95f, AttackSeconds=1.4f, KillGold=8, Loot=new[]{LootKind.Bracer} },
            new UnitDefinition { Id=Kind.Lizard, DisplayName="리자드", Health=65, Damage=12, Range=5, MoveSpeed=2.2f, AttackSeconds=1.5f, Vision=10, KillGold=4, Loot=new[]{LootKind.Tail} },
            new UnitDefinition { Id=Kind.Animal, DisplayName="사슴", Health=25, MoveSpeed=.6f, Loot=new[]{LootKind.Meat,LootKind.Fur} }
        };
    }
    [Serializable] public sealed class WaveDefinition
    {
        public int Count, Groups=4, Fronts=3;
        public float GroupInterval=10;
        public string Description;
        public Kind[] Pattern;
    }
    [Serializable] public sealed class WaveRules
    {
        public float HealthGrowth=.12f, DamageGrowth=.18f, SpawnDistance=10.5f, SpawnDistanceVariation=5;
        public List<WaveDefinition> Entries = new List<WaveDefinition>
        {
            new WaveDefinition { Count=6, Groups=3, Fronts=1, GroupInterval=12, Description="슬라임 · 한 방향", Pattern=new[]{Kind.Slime} },
            new WaveDefinition { Count=10, Groups=3, Fronts=1, GroupInterval=12, Description="빠른 고블린 합류", Pattern=new[]{Kind.Slime,Kind.Goblin,Kind.Goblin} },
            new WaveDefinition { Count=16, Fronts=2, Description="고블린 전사·주술사 · 양방향", Pattern=new[]{Kind.GoblinShaman,Kind.GoblinWarrior,Kind.Goblin,Kind.Goblin} },
            new WaveDefinition { Count=24, Fronts=2, Description="오크 돌격 + 주술사 지원", Pattern=new[]{Kind.Orc,Kind.GoblinShaman,Kind.GoblinWarrior,Kind.Goblin,Kind.Goblin} },
            new WaveDefinition { Count=32, Description="리자드 궁수 · 세 방향", Pattern=new[]{Kind.GoblinShaman,Kind.Lizard,Kind.Orc,Kind.GoblinWarrior,Kind.Goblin} },
            new WaveDefinition { Count=42, Description="총공세 · 중장갑과 원거리 혼성", Pattern=new[]{Kind.GoblinShaman,Kind.Lizard,Kind.Orc,Kind.GoblinWarrior,Kind.Orc} }
        };
        public WaveDefinition At(int day) => Entries[Math.Max(0,Math.Min(day-1,Entries.Count-1))];
    }
    [Serializable] public sealed class ResourceRules
    {
        public float TreeStock=70, RockStock=50, ValuableChance=.4f, EcologySeconds=12;
        public int TreeGold=1, RockGold=2, ValuableGold=5, RegrowthHeadroom=30, InitialNatureCount=55;
        public float RegrowthMinimumDistance=4, InitialNatureDistance=6, AnimalMinimumDistance=5, SlimeMinimumDistance=12;
        public int PreferredBiomeDensity=90, SnowBudgetDivisor=5, GrassGroupBudgetDivisor=12, GrassSinglePercent=3;
        public float GrassGroupSpacing=5.5f, GrassSingleGroupSpacing=3.6f, GrassSingleSpacing=3;
        public string[] TreeNames={"둥근나무","침엽수","자작나무","참나무"};
        public string[] RockNames={"일반 바위","층리암","수정 바위","화강암"};
        public string[] AnimalNames={"사슴","토끼","멧돼지","여우"};
    }
    [Serializable] public sealed class EconomyRules
    {
        public int BasePopulation=1, MaxQueuedOrders=10;
        public float EfficiencyPerResident=.09f, RadiusPerResident=.04f, RepairPerGold=10, ShrineConstructionMultiplier=1.25f, RuinsRangeBonus=1;
    }
    [Serializable] public sealed class CombatRules
    {
        public float AttackWindup=.18f, AttackRecovery=.18f, HitStun=.2f, AnimalFleeSeconds=5, AnimalFleeMultiplier=4.2f;
        public float FriendlyVision=5, TowerVisionBonus=.65f, FriendlyCriticalChance=.05f, CriticalMultiplier=2;
        public float LightningDamage=24, ChainDamageMultiplier=.5f, ChainRange=3, ShockSeconds=3, AbundanceShieldSeconds=10, EchoSeconds=10, HarvestRefundCap=3;
        public float BurnTickSeconds=.5f, TacticsSeconds=.65f, AttackMoveAcquireRange=4.5f, ManualGuardRange=6, AutoGuardRange=8;
    }
    [Serializable] public sealed class GrowthRules
    {
        public int MaxLevel=5, CostPerLevel=30, TowerMaxLevel=5, TowerCostPerLevel=40;
        public float PulseMultiplier=.84f, MinimumPulseSeconds=.75f, ConstructionPerLevel=.5f, WallHealthPerLevel=.25f;
        public float CriticalChancePerLevel=.05f, CriticalPowerPerLevel=.5f, TowerDamagePerLevel=.2f, TowerRangePerLevel=.35f, TowerResearchRangePerLevel=.5f;
        public float IgniteChancePerLevel=.05f, FireImpact=8, FireImpactPerLevel=.3f, BurnDamage=3, BurnDamagePerLevel=.4f, BurnSeconds=3, BurnSecondsPerLevel=1;
    }
    [Serializable] public sealed class RebirthRules
    {
        public float WorkPerLevel=.2f, ArmyPerLevel=.2f, HouseHealthPerLevel=.25f;
        public int WaveReward=2, BonusCapDivisor=2, KillsPerBonus=10, GoldPerBonus=100, MemoryBasePrice=8, MemoryMaxLevel=3, BannerUnlockDay=2;
    }
    [Serializable] public sealed class RelicRules
    {
        public float FieldRadius=.15f, SlotRecharge=.1f;
        public int FieldRadiusMaxLevel=3, SlotRechargeMaxLevel=3, FieldSlotMaxLevel=1;
        public float Wood=.5f, Stone=.3f, Critical=.15f, Repair=.3f, Trade=.25f, ResourceCapacity=.5f, RegrowthMultiplier=.7f, MinimumRegrowthSeconds=1;
        public int DailyChoices=3;
        public string Describe(RelicKind kind,int level)
        {
            switch(kind)
            {
                case RelicKind.FieldRadius:return $"필드 반경 +{FieldRadius*100*level:0.#}%";
                case RelicKind.SlotRecharge:return $"슬롯 충전 시간 -{SlotRecharge*100*level:0.#}% (연구 합산 최대 50%)";
                case RelicKind.FieldSlot:return $"필드 충전 슬롯 +{level}개 (총 최대 3개)";
                case RelicKind.Wood:return $"목재 채집 기여 +{Wood*100*level:0.#}%";
                case RelicKind.Stone:return $"석재 채집 기여 +{Stone*100*level:0.#}%";
                case RelicKind.Critical:return $"커서·아군 치명타 확률 +{Math.Min(100,Critical*100*level):0.#}%p (총 100% 상한)";
                case RelicKind.Repair:return $"커서 수리 +{Repair*100*level:0.#}%";
                case RelicKind.Trade:return $"채집 골드 획득 +{Trade*100*level:0.#}%";
                case RelicKind.ResourceCapacity:return $"나무·바위 생성 상한 +{ResourceCapacity*100*level:0.#}%";
                default:return $"나무·바위 생성 간격 ×{Math.Pow(RegrowthMultiplier,level):0.00} (최소 {MinimumRegrowthSeconds:0.#}초)";
            }
        }
    }
    [Serializable] public sealed class LootDefinition { public LootKind Id; public string DisplayName; public int Value; }
    [Serializable] public sealed class LootRules
    {
        public List<LootDefinition> Entries = new List<LootDefinition>
        {
            new LootDefinition { Id=LootKind.SlimeGel,DisplayName="슬라임 진액",Value=1 },new LootDefinition { Id=LootKind.Club,DisplayName="몽둥이",Value=2 },
            new LootDefinition { Id=LootKind.Bone,DisplayName="뼈",Value=1 },new LootDefinition { Id=LootKind.Armor,DisplayName="갑주",Value=4 },
            new LootDefinition { Id=LootKind.Staff,DisplayName="지팡이",Value=5 },new LootDefinition { Id=LootKind.Bracer,DisplayName="팔보호대",Value=6 },
            new LootDefinition { Id=LootKind.Tail,DisplayName="리자드 꼬리",Value=4 },new LootDefinition { Id=LootKind.Meat,DisplayName="고기",Value=2 },new LootDefinition { Id=LootKind.Fur,DisplayName="모피",Value=2 }
        };
        public LootDefinition Find(LootKind id) => Entries.Find(x=>x.Id==id) ?? throw new ArgumentException("전리품 데이터 누락: "+id);
    }
    [Serializable] public sealed class ChestDefinition
    {
        public ChestGrade Id;
        public string DisplayName;
        public float Durability;
        public int Weight, ItemCount, Gold, WoodGold, StoneGold, DuplicateGold;
        public RelicKind[] Relics;
        public LootKind[] Items;
    }
    [Serializable] public sealed class ChestRules
    {
        public int InitialCount=4, MaximumCount=7;
        public float InitialDelay=50, SpawnSeconds=45, SpawnVariation=35, MinimumDistance=6;
        public List<ChestDefinition> Entries = new List<ChestDefinition>
        {
            new ChestDefinition { Id=ChestGrade.Common,DisplayName="일반 상자",Durability=12,Weight=60,ItemCount=2,Gold=25,WoodGold=40,StoneGold=60,DuplicateGold=35, Relics=new[]{RelicKind.Wood,RelicKind.Repair}, Items=new[]{LootKind.SlimeGel,LootKind.Club,LootKind.Bone,LootKind.Meat,LootKind.Fur} },
            new ChestDefinition { Id=ChestGrade.Rare,DisplayName="희귀 상자",Durability=36,Weight=30,ItemCount=3,Gold=50,WoodGold=80,StoneGold=120,DuplicateGold=70, Relics=new[]{RelicKind.Wood,RelicKind.Repair,RelicKind.Stone,RelicKind.Trade,RelicKind.ResourceCapacity}, Items=new[]{LootKind.Armor,LootKind.Bracer,LootKind.Tail} },
            new ChestDefinition { Id=ChestGrade.Ancient,DisplayName="고대 상자",Durability=72,Weight=10,ItemCount=4,Gold=100,WoodGold=160,StoneGold=240,DuplicateGold=140, Relics=(RelicKind[])Enum.GetValues(typeof(RelicKind)), Items=new[]{LootKind.Staff,LootKind.Bracer,LootKind.Armor} }
        };
        public ChestDefinition Find(ChestGrade id) => Entries.Find(x=>x.Id==id) ?? throw new ArgumentException("상자 데이터 누락: "+id);
    }
    [Serializable] public sealed class PrototypeRules
    {
        public UnitRules Units=new UnitRules();
        public WaveRules Waves=new WaveRules();
        public ResourceRules Resources=new ResourceRules();
        public EconomyRules Economy=new EconomyRules();
        public CombatRules Combat=new CombatRules();
        public GrowthRules Growth=new GrowthRules();
        public RebirthRules Rebirth=new RebirthRules();
        public RelicRules Relics=new RelicRules();
        public LootRules Loot=new LootRules();
        public ChestRules Chests=new ChestRules();
        public void Validate()
        {
            ValidateNumbers(this,"Rules");
            if(Economy.MaxQueuedOrders<0||Economy.MaxQueuedOrders>10||Relics.DailyChoices<1||Relics.DailyChoices>3)throw new ArgumentException("현재 UI는 생산 예약 0~10개, 일일 보상 1~3개를 지원합니다.");
            if(Rebirth.MemoryMaxLevel<0||Rebirth.MemoryMaxLevel>20)throw new ArgumentException("기억 강화 최대 단계는 0~20이어야 합니다.");
            if(Units.Entries.Any(x=>x==null) || Units.Entries.Select(x=>x.Id).Distinct().Count()!=Units.Entries.Count)throw new ArgumentException("유닛 ID 누락/중복");
            foreach(var id in new UnitRules().Entries.Select(x=>x.Id))
            {var u=Units.Find(id);if(u.Health<=0||u.AttackSeconds<=0||u.MoveSpeed<0||u.Price<0||u.RecruitmentMultiplier<=0||u.Loot==null)throw new ArgumentException("유닛 수치 오류: "+id);}
            if(Waves.Entries.Count==0||Waves.Entries.Any(x=>x==null||x.Count<0||x.Groups<1||x.Fronts<1||x.GroupInterval<=0||x.Pattern==null||x.Pattern.Length==0))throw new ArgumentException("웨이브 구성 오류");
            foreach(var wave in Waves.Entries)foreach(var id in wave.Pattern){Units.Find(id);if(id!=Kind.Slime&&(id<Kind.Goblin||id>Kind.Lizard))throw new ArgumentException("웨이브 적 ID 오류: "+id);}
            if(Loot.Entries.Any(x=>x==null)||Loot.Entries.Select(x=>x.Id).Distinct().Count()!=Loot.Entries.Count)throw new ArgumentException("전리품 ID 누락/중복");
            if(Chests.Entries.Any(x=>x==null)||Chests.Entries.Select(x=>x.Id).Distinct().Count()!=Chests.Entries.Count)throw new ArgumentException("상자 ID 누락/중복");
            foreach(LootKind id in Enum.GetValues(typeof(LootKind)))if(Loot.Find(id).Value<0)throw new ArgumentException("전리품 가격 오류");
            foreach(ChestGrade id in Enum.GetValues(typeof(ChestGrade)))
            {var c=Chests.Find(id);if(c.Durability<=0||c.Weight<0||c.Items==null||c.Items.Length==0||c.Relics==null)throw new ArgumentException("상자 구성 오류: "+id);}
            if(Chests.Entries.Sum(x=>x.Weight)<=0||Rebirth.BonusCapDivisor<=0||Rebirth.KillsPerBonus<=0||Rebirth.GoldPerBonus<=0||Economy.RepairPerGold<=0||Resources.SnowBudgetDivisor<=0||Resources.GrassGroupBudgetDivisor<=0||Relics.RegrowthMultiplier<=0||Growth.MinimumPulseSeconds<=0)throw new ArgumentException("시스템 분모/간격은 양수여야 합니다.");
            if(Resources.TreeNames==null||Resources.TreeNames.Length!=4||Resources.RockNames==null||Resources.RockNames.Length!=4||Resources.AnimalNames==null||Resources.AnimalNames.Length!=4)throw new ArgumentException("자원 이름은 종류별 4개가 필요합니다.");
        }
        private static void ValidateNumbers(object value,string path)
        {
            if(value==null)throw new ArgumentException("데이터 누락: "+path);
            if(value is float number){if(float.IsNaN(number)||float.IsInfinity(number)||number<0)throw new ArgumentException("0 이상의 유한 수치가 필요합니다: "+path);return;}
            if(value is int integer){if(integer<0)throw new ArgumentException("0 이상의 정수가 필요합니다: "+path);return;}
            if(value is string||value.GetType().IsEnum||value is bool)return;
            if(value is System.Collections.IEnumerable entries){foreach(var item in entries)ValidateNumbers(item,path+"[]");return;}
            foreach(var field in value.GetType().GetFields())ValidateNumbers(field.GetValue(value),path+"."+field.Name);
        }
    }
}
