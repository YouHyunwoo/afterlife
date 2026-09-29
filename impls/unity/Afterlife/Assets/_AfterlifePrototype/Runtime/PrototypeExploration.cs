using System;
using System.Collections.Generic;
using System.Linq;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        public readonly Dictionary<LootKind,int> Loot=new Dictionary<LootKind,int>();
        public readonly HashSet<RelicKind> Relics=new HashSet<RelicKind>();
        public event Action RelicsChanged;
        private readonly Dictionary<RelicKind,int> _relicRanks=new Dictionary<RelicKind,int>();
        private readonly List<RelicKind> _dailyRelicChoices=new List<RelicKind>();
        public IReadOnlyList<RelicKind> DailyRelicChoices=>_dailyRelicChoices;
        public bool DailyRewardPending {get;private set;}
        public bool DailyRewardChoosing {get;private set;}
        public int RelicLevel(RelicKind kind)=>!Relics.Contains(kind)?0:_relicRanks.TryGetValue(kind,out int rank)?rank:1;
        public string RelicEffect(RelicKind kind,int level)=>Rules.Relics.Describe(kind,level);
        private void PrepareDailyRelics()
        {
            _dailyRelicChoices.Clear();DailyRewardPending=true;DailyRewardChoosing=false;
            var pool=((RelicKind[])Enum.GetValues(typeof(RelicKind))).Where(CanGainDailyRelic).ToList();
            // 미보유 우선, 같은 선택지 중복 없음. 추첨은 결산 진입 때 한 번만 한다.
            for(int i=0;i<Rules.Relics.DailyChoices&&pool.Count>0;i++)
            {
                var available=pool.Where(k=>!Relics.Contains(k)).ToArray();
                var candidates=available.Length>0?available:pool.ToArray();
                var kind=candidates[_random.Next(candidates.Length)];_dailyRelicChoices.Add(kind);pool.Remove(kind);
            }
        }
        public void OpenDailyReward()
        {if(Phase==Phase.Settlement&&DailyRewardPending){if(!DailyRewardChoosing)Sound(SoundCue.Open);DailyRewardChoosing=true;}}
        public bool ChooseDailyRelic(int index)
        {
            if(Phase!=Phase.Settlement||!DailyRewardPending||!DailyRewardChoosing||index<0||index>=_dailyRelicChoices.Count)return false;
            var kind=_dailyRelicChoices[index];int rank=RelicLevel(kind)+1;
            Relics.Add(kind);_relicRanks[kind]=rank;DailyRewardPending=DailyRewardChoosing=false;_dailyRelicChoices.Clear();
            RefreshFieldSlots();RelicsChanged?.Invoke();
            Sound(SoundCue.Relic);
            Say($"생존 보상 · {RelicName(kind)} {rank}단계 · {RelicEffect(kind,rank)}");return true;
        }
        public string LastLootSummary {get;private set;}="";
        public bool ActiveShrine {get;private set;}
        public bool ActiveRuins {get;private set;}
        private float _chestClock;
        public static bool IsBuildingKind(Kind kind)=>kind<=Kind.Altar||kind==Kind.ArrowTower||kind==Kind.MagicTower||kind==Kind.Wall||kind==Kind.TownHall||kind==Kind.Sanctuary||kind==Kind.ArcheryRange||kind==Kind.KnightHall||kind==Kind.MageTower;
        public string LootName(LootKind kind)=>Rules.Loot.Find(kind).DisplayName;
        public static string RelicName(RelicKind kind)=>kind==RelicKind.FieldRadius?"넓어지는 손길":kind==RelicKind.SlotRecharge?"빠른 재충전":kind==RelicKind.FieldSlot?"추가 필드 슬롯":kind==RelicKind.Wood?"숲의 도끼":kind==RelicKind.Stone?"광부의 부적":kind==RelicKind.Critical?"매의 눈":kind==RelicKind.Repair?"수선의 망치":kind==RelicKind.Trade?"상인의 저울":kind==RelicKind.ResourceCapacity?"풍요의 씨앗":"순환의 모래시계";
        private bool CanGainDailyRelic(RelicKind kind)
        {
            int level = RelicLevel(kind);
            if (kind >= RelicKind.FieldRadius && !Settings.MvpSlots) return false;
            switch (kind)
            {
                case RelicKind.Critical:
                    return !Settings.MvpSlots || CursorCriticalChance < 1 || Rules.Combat.FriendlyCriticalChance + Rules.Relics.Critical * level < 1;
                case RelicKind.FieldRadius: return level < Rules.Relics.FieldRadiusMaxLevel && Rules.Relics.FieldRadius > 0;
                case RelicKind.SlotRecharge: return level < Rules.Relics.SlotRechargeMaxLevel && SlotReduction < .5f && Rules.Relics.SlotRecharge > 0;
                case RelicKind.FieldSlot: return level < Rules.Relics.FieldSlotMaxLevel && FieldSlots.Slots.Count < Settings.MaximumSlots;
                case RelicKind.ResourceRegrowth:
                    return !Settings.MvpSlots || Math.Max(Rules.Relics.MinimumRegrowthSeconds, Settings.ResourceSpawnSeconds*(float)Math.Pow(Rules.Relics.RegrowthMultiplier,level+1)) < ResourceSpawnSeconds;
                default: return true;
            }
        }
        public string RelicEffect(RelicKind kind)=>RelicEffect(kind,1);
        public string ChestName(ChestGrade grade)=>Rules.Chests.Find(grade).DisplayName;
        public float ChestDurability(ChestGrade grade)=>Rules.Chests.Find(grade).Durability;
        public RelicKind[] ChestRelics(ChestGrade grade)=>Rules.Chests.Find(grade).Relics.Where(kind=>kind<RelicKind.FieldRadius).ToArray();
        public LootKind[] ChestItems(ChestGrade grade)=>Rules.Chests.Find(grade).Items;
        public float HarvestGoldMultiplier=>1+Rules.Relics.Trade*RelicLevel(RelicKind.Trade);
        public float CursorCriticalChance=>Math.Min(1,Rules.Growth.CriticalChancePerLevel*CriticalChanceLevel+Rules.Relics.Critical*RelicLevel(RelicKind.Critical));
        public float CursorCriticalMultiplier=>Rules.Combat.CriticalMultiplier+Rules.Growth.CriticalPowerPerLevel*CriticalPowerLevel;
        private float RollContribution(out bool critical){critical=CursorCriticalChance>0&&_random.NextDouble()<CursorCriticalChance;return critical?CursorCriticalMultiplier:1;}
        private float _resourceClock;
        private int _baseTreeLimit,_baseRockLimit;
        public int ResourceLimit(Kind kind)=>(int)Math.Ceiling((kind==Kind.Tree?Math.Max(Settings.TreeLimit,_baseTreeLimit):Math.Max(Settings.RockLimit,_baseRockLimit))*(1+Rules.Relics.ResourceCapacity*RelicLevel(RelicKind.ResourceCapacity)));
        public float ResourceSpawnSeconds=>Math.Max(Rules.Relics.MinimumRegrowthSeconds,Settings.ResourceSpawnSeconds*(float)Math.Pow(Rules.Relics.RegrowthMultiplier,RelicLevel(RelicKind.ResourceRegrowth)));
        private void InitializeResourceLimits(){_baseTreeLimit=Math.Max(Settings.TreeLimit,Bodies.Count(b=>b.Alive&&b.Kind==Kind.Tree)+Rules.Resources.RegrowthHeadroom);_baseRockLimit=Math.Max(Settings.RockLimit,Bodies.Count(b=>b.Alive&&b.Kind==Kind.Rock)+Rules.Resources.RegrowthHeadroom);}
        private void ProcessResourceRegrowth(float dt)
        {
            _resourceClock+=dt;if(_resourceClock<ResourceSpawnSeconds)return;_resourceClock-=ResourceSpawnSeconds;
            SpawnNature(Kind.Tree,Rules.Resources.RegrowthMinimumDistance);SpawnNature(Kind.Rock,Rules.Resources.RegrowthMinimumDistance);
        }
        public int LootValue(LootKind kind)=>Rules.Loot.Find(kind).Value;
        public int PendingLootMoney=>Loot.Sum(pair=>LootValue(pair.Key)*pair.Value);
        private void AddLoot(LootKind kind,int count=1){if(!Loot.ContainsKey(kind))Loot[kind]=0;Loot[kind]+=count;}
        private void CollectKillLoot(Body body)
        {
            if(!body.Unit)return;
            var data=Rules.Units.Find(body.Kind);
            if(body.Hostile)
            {
                int coins=data.KillGold;Money+=coins;EmitText(body,$"+{coins} 골드",FeedbackKind.Normal);Sound(SoundCue.Gold,body.Position);
            }
            foreach(var item in data.Loot)AddLoot(item);
        }
        private int SellLoot()
        {
            int value=PendingLootMoney;LastLootSummary=string.Join(" · ",Loot.Select(pair=>$"{LootName(pair.Key)} {pair.Value}"));Loot.Clear();return value;
        }
        private void ResetExploration(){Loot.Clear();Relics.Clear();_relicRanks.Clear();_dailyRelicChoices.Clear();DailyRewardPending=DailyRewardChoosing=false;LastLootSummary="";ActiveShrine=ActiveRuins=false;_chestClock=Rules.Chests.InitialDelay;_resourceClock=0;_baseTreeLimit=_baseRockLimit=0;RelicsChanged?.Invoke();}
        private void SeedDiscoveries()
        {
            for(int i=0;i<Terrain.Layout.Landmarks.Count;i++)
            {
                var point=Terrain.Layout.Landmarks[i];
                Add(i%2==0?Kind.AbandonedAltar:Kind.Ruins,new Point(point.X,point.Z),9999);
            }
            for(int i=0;i<Rules.Chests.InitialCount;i++)SpawnChest();
        }
        private void SpawnChest()
        {
            if(Bodies.Count(b=>b.Alive&&b.Kind==Kind.Chest)>=Rules.Chests.MaximumCount)return;
            for(int i=0;i<80;i++)
            {
                var p=new Point(_random.Next(-Settings.MapRadius+3,Settings.MapRadius-2),_random.Next(-Settings.MapRadius+3,Settings.MapRadius-2));
                if(Point.Distance(p,new Point(0,0))<Rules.Chests.MinimumDistance||!Walkable(p,.65f)||!UnitFree(p,0))continue;
                int roll=_random.Next(Rules.Chests.Entries.Sum(entry=>entry.Weight));var grade=Rules.Chests.Entries.Last().Id;foreach(var entry in Rules.Chests.Entries){roll-=entry.Weight;if(roll<0){grade=entry.Id;break;}}
                var chest=Add(Kind.Chest,p,ChestDurability(grade));chest.ChestGrade=grade;chest.Yaw=(float)_random.NextDouble()*360;return;
            }
        }
        private void ProcessExploration(float dt)
        {
            if(Phase==Phase.Day){_chestClock-=dt;if(_chestClock<=0){_chestClock=Rules.Chests.SpawnSeconds+(float)_random.NextDouble()*Rules.Chests.SpawnVariation;SpawnChest();}}
        }
        private void WorkChest(Body chest,float effort,FeedbackKind feedback)
        {
            if(!chest.Alive||chest.ChestOpened)return;
            float amount=Math.Min(chest.Health,effort);chest.Health-=amount;
            if(chest.Health>0){EmitText(chest,$"개봉 -{amount:0.#}",feedback);return;}
            chest.ChestOpened=true;int reward=_random.Next(5);var definition=Rules.Chests.Find(chest.ChestGrade);string text;
            Sound(SoundCue.Chest,chest.Position);
            if(reward==0){var pool=ChestItems(chest.ChestGrade);var item=pool[_random.Next(pool.Length)];int count=definition.ItemCount;AddLoot(item,count);text=$"{LootName(item)} ×{count} (새벽 판매)";}
            else if(reward==1){int amountMoney=definition.Gold;Money+=amountMoney;text=$"골드 +{amountMoney}";}
            else if(reward==2){int amountWood=definition.WoodGold;Money+=amountWood;text=$"목재 판매 · 골드 +{amountWood}";}
            else if(reward==3){int amountStone=definition.StoneGold;Money+=amountStone;text=$"석재 판매 · 골드 +{amountStone}";}
            else
            {
                // 등급 허용 풀과 미보유 목록의 교집합 안에서 직접 추첨한다. 중복 재추첨은 하지 않는다.
                var available=ChestRelics(chest.ChestGrade).Where(k=>!Relics.Contains(k)).ToArray();
                if(available.Length==0){int bonus=definition.DuplicateGold;Money+=bonus;text=$"이 등급 유물 모두 보유 · 골드 +{bonus}";}
                else{var relic=available[_random.Next(available.Length)];Relics.Add(relic);RelicsChanged?.Invoke();Sound(SoundCue.Relic);text=$"유물: {RelicName(relic)} · {RelicEffect(relic)}";}
            }
            EmitText(chest,text);Say(ChestName(chest.ChestGrade)+" · "+text);
            if(reward>=1&&reward<=3)Sound(SoundCue.Gold,chest.Position);
        }
        private void UpdateCapturedSites()
        {
            bool shrine=Bodies.Any(b=>b.Alive&&b.Kind==Kind.AbandonedAltar&&Territory.Contains(TileKey((int)b.Position.X,(int)b.Position.Z)));
            bool ruins=Bodies.Any(b=>b.Alive&&b.Kind==Kind.Ruins&&Territory.Contains(TileKey((int)b.Position.X,(int)b.Position.Z)));
            if(shrine&&!ActiveShrine||ruins&&!ActiveRuins)Sound(SoundCue.Relic);
            if(shrine!=ActiveShrine||ruins!=ActiveRuins)Say("마을 편입 효과 · "+(shrine?$"제단: 자동 건설 +{(Rules.Economy.ShrineConstructionMultiplier-1)*100:0.#}% ":"")+(ruins?$"유적: 타워 사거리 +{Rules.Economy.RuinsRangeBonus:0.#}":""));
            ActiveShrine=shrine;ActiveRuins=ruins;
        }
        private float CriticalDamage(float damage)=>damage*(_random.NextDouble()<Math.Min(1,Rules.Combat.FriendlyCriticalChance+Rules.Relics.Critical*RelicLevel(RelicKind.Critical))?Rules.Combat.CriticalMultiplier:1);
        private void Flee(Body animal,float dt)
        {
            if(animal.Route.Count==0||animal.RouteTimer<=0)
            {
                float baseAngle=(float)Math.Atan2(animal.Position.Z-animal.FleeFrom.Z,animal.Position.X-animal.FleeFrom.X);
                for(int i=0;i<12;i++)
                {
                    float angle=baseAngle+(float)(_random.NextDouble()-.5)*2.4f;
                    var goal=new Point(animal.Position.X+(float)Math.Cos(angle)*5,animal.Position.Z+(float)Math.Sin(angle)*5);
                    if(Walkable(goal,.35f)){animal.Destination=goal;break;}
                }
            }
            Navigate(animal,animal.Destination,null,.3f,dt);
        }
        public int Connections(Body body)
        {
            int mask=0;foreach(var other in Bodies)
            {
                if(other==body||!other.Alive||other.Kind!=body.Kind)continue;
                float x=other.Position.X-body.Position.X,z=other.Position.Z-body.Position.Z;
                if(Math.Abs(x)<.1&&Math.Abs(z-1)<.1)mask|=1;if(Math.Abs(x-1)<.1&&Math.Abs(z)<.1)mask|=2;
                if(Math.Abs(x)<.1&&Math.Abs(z+1)<.1)mask|=4;if(Math.Abs(x+1)<.1&&Math.Abs(z)<.1)mask|=8;
            }return mask;
        }
        public string MoveError(Body body,int x,int z)
        {
            if(body==null||!body.Building||!body.Complete||body.MovingOut||body.MoveSource!=0)return "완공된 건물만 이동할 수 있습니다.";
            var center=new Point(x+(body.Width-1)*.5f,z+(body.Depth-1)*.5f);
            if(Point.Distance(center,body.Position)<.1f)return "현재 위치입니다.";
            for(int dx=0;dx<body.Width;dx++)for(int dz=0;dz<body.Depth;dz++)
            {
                var p=new Point(x+dx,z+dz);
                if(Terrain.At(p)==TerrainKind.Water||body.Kind!=Kind.House&&!Territory.Contains(TileKey(x+dx,z+dz)))return "마을 영역의 육지에 놓으세요.";
                if(Bodies.Any(b=>b.Alive&&(b.Building?b.DistanceTo(p)<.49f:Point.Distance(b.Position,p)<.8f)))return "목적지가 다른 오브젝트와 겹칩니다.";
            }return null;
        }
        public void BeginRelocation(int id,int x,int z)
        {
            var body=Find(id);string error=MoveError(body,x,z);if(Frozen||error!=null){if(error!=null){Sound(SoundCue.Error);Say(error);}return;}
            var site=AddBuilding(body.Kind,x,z,false);site.PaidGold=0;site.Health=body.Health;site.MaxHealth=body.MaxHealth;site.MoveSource=id;
            body.MovingOut=true;body.MoveSite=site.Id;body.MoveProgress=0;TopologyRevision++;Recalculate();InvalidatePaths();Say("건물 이전 시작 · 원위치 철거/목적지 재건에 커서 지원 가능");
            Sound(SoundCue.Place);
        }
        private void AdvanceMoves(float dt)
        {foreach(var body in Bodies)if(body.Alive&&body.MovingOut)AdvanceMove(body,AutoBuildMultiplier*LocalConstructionMultiplier(Find(body.MoveSite)??body)*dt/(body.ConstructionSeconds*ConstructionWorkFactor));}
        private void AdvanceMove(Body body,float progress)
        {
            if(body==null||!body.MovingOut)return;var site=Find(body.MoveSite);if(site==null)return;
            body.MoveProgress=Math.Min(1,body.MoveProgress+progress);body.Construction=1-body.MoveProgress;site.Construction=body.MoveProgress;
            if(body.MoveProgress<1)return;
            site.MoveSource=0;TransferProduction(body.Id,site.Id);site.TowerLevel=body.TowerLevel;site.Health=Math.Min(site.Health,body.Health);while(body.Orders.Count>0)site.Orders.Enqueue(body.Orders.Dequeue());
            foreach(var unit in Bodies)if(unit.Owner==body.Id)unit.Owner=site.Id;
            body.MoveSite=0;body.MovingOut=false;body.Health=0;TopologyRevision++;Recalculate();InvalidatePaths();
            Sound(SoundCue.Complete);
        }
        private void AbortDestroyedMove(Body destroyed)
        {
            if(destroyed.MoveSite!=0){var site=Find(destroyed.MoveSite);if(site!=null){site.MoveSource=0;site.Health=0;}destroyed.MoveSite=0;}
            if(destroyed.MoveSource!=0){var origin=Find(destroyed.MoveSource);if(origin!=null){origin.MoveSite=0;origin.MovingOut=false;origin.Construction=1;origin.MoveProgress=0;}destroyed.MoveSource=0;}
        }
    }
}
