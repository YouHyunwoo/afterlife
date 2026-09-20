using System;
using System.Collections.Generic;
using System.Linq;
namespace Afterlife.Prototype
{
    public enum LootKind { SlimeGel,Club,Bone,Armor,Staff,Bracer,Tail,Meat,Fur }
    public enum RelicKind { Wood,Stone,Critical,Repair,Trade,ResourceCapacity,ResourceRegrowth }
    public enum ChestGrade { Common,Rare,Ancient }
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
        public static string RelicEffect(RelicKind kind,int level)
        {
            switch(kind)
            {
                case RelicKind.Wood:return $"목재 채집 기여 +{50*level}%";
                case RelicKind.Stone:return $"석재 채집 기여 +{30*level}%";
                case RelicKind.Critical:return $"커서·아군 치명타 확률 +{Math.Min(100,15*level)}%p (총 100% 상한)";
                case RelicKind.Repair:return $"커서 수리 +{30*level}%";
                case RelicKind.Trade:return $"채집 골드 획득 +{25*level}%";
                case RelicKind.ResourceCapacity:return $"나무·바위 생성 상한 +{50*level}%";
                default:return $"나무·바위 생성 간격 ×{Math.Pow(.7,level):0.00} (최소 1초)";
            }
        }
        private void PrepareDailyRelics()
        {
            _dailyRelicChoices.Clear();DailyRewardPending=true;DailyRewardChoosing=false;
            var pool=((RelicKind[])Enum.GetValues(typeof(RelicKind))).ToList();
            // 미보유 우선, 같은 선택지 중복 없음. 추첨은 결산 진입 때 한 번만 한다.
            for(int i=0;i<3&&pool.Count>0;i++)
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
            RelicsChanged?.Invoke();
            Sound(SoundCue.Relic);
            Say($"생존 보상 · {RelicName(kind)} {rank}단계 · {RelicEffect(kind,rank)}");return true;
        }
        public string LastLootSummary {get;private set;}="";
        public bool ActiveShrine {get;private set;}
        public bool ActiveRuins {get;private set;}
        private float _chestClock;
        public static bool IsBuildingKind(Kind kind)=>kind<=Kind.Altar||kind==Kind.ArrowTower||kind==Kind.MagicTower||kind==Kind.Wall;
        public static string LootName(LootKind kind)=>kind==LootKind.SlimeGel?"슬라임 진액":kind==LootKind.Club?"몽둥이":kind==LootKind.Bone?"뼈":kind==LootKind.Armor?"갑주":kind==LootKind.Staff?"지팡이":kind==LootKind.Bracer?"팔보호대":kind==LootKind.Tail?"리자드 꼬리":kind==LootKind.Meat?"고기":"모피";
        public static string RelicName(RelicKind kind)=>kind==RelicKind.Wood?"숲의 도끼":kind==RelicKind.Stone?"광부의 부적":kind==RelicKind.Critical?"매의 눈":kind==RelicKind.Repair?"수선의 망치":kind==RelicKind.Trade?"상인의 저울":kind==RelicKind.ResourceCapacity?"풍요의 씨앗":"순환의 모래시계";
        public static string RelicEffect(RelicKind kind)=>RelicEffect(kind,1);
        public static string ChestName(ChestGrade grade)=>grade==ChestGrade.Common?"일반 상자":grade==ChestGrade.Rare?"희귀 상자":"고대 상자";
        public static float ChestDurability(ChestGrade grade)=>grade==ChestGrade.Common?12:grade==ChestGrade.Rare?36:72;
        public static RelicKind[] ChestRelics(ChestGrade grade)=>grade==ChestGrade.Common?new[]{RelicKind.Wood,RelicKind.Repair}:grade==ChestGrade.Rare?new[]{RelicKind.Wood,RelicKind.Repair,RelicKind.Stone,RelicKind.Trade,RelicKind.ResourceCapacity}:(RelicKind[])Enum.GetValues(typeof(RelicKind));
        public static LootKind[] ChestItems(ChestGrade grade)=>grade==ChestGrade.Common?new[]{LootKind.SlimeGel,LootKind.Club,LootKind.Bone,LootKind.Meat,LootKind.Fur}:grade==ChestGrade.Rare?new[]{LootKind.Armor,LootKind.Bracer,LootKind.Tail}:new[]{LootKind.Staff,LootKind.Bracer,LootKind.Armor};
        public float HarvestGoldMultiplier=>1+.25f*RelicLevel(RelicKind.Trade);
        public float CursorCriticalChance=>Math.Min(1,.05f*CriticalChanceLevel+.15f*RelicLevel(RelicKind.Critical));
        public float CursorCriticalMultiplier=>2+.5f*CriticalPowerLevel;
        private float RollContribution(out bool critical){critical=CursorCriticalChance>0&&_random.NextDouble()<CursorCriticalChance;return critical?CursorCriticalMultiplier:1;}
        private float _resourceClock;
        private int _baseTreeLimit,_baseRockLimit;
        public int ResourceLimit(Kind kind)=>(int)Math.Ceiling((kind==Kind.Tree?Math.Max(Settings.TreeLimit,_baseTreeLimit):Math.Max(Settings.RockLimit,_baseRockLimit))*(1+.5f*RelicLevel(RelicKind.ResourceCapacity)));
        public float ResourceSpawnSeconds=>Math.Max(1,Settings.ResourceSpawnSeconds*(float)Math.Pow(.7,RelicLevel(RelicKind.ResourceRegrowth)));
        private void InitializeResourceLimits(){_baseTreeLimit=Math.Max(Settings.TreeLimit,Bodies.Count(b=>b.Alive&&b.Kind==Kind.Tree)+30);_baseRockLimit=Math.Max(Settings.RockLimit,Bodies.Count(b=>b.Alive&&b.Kind==Kind.Rock)+30);}
        private void ProcessResourceRegrowth(float dt)
        {
            _resourceClock+=dt;if(_resourceClock<ResourceSpawnSeconds)return;_resourceClock-=ResourceSpawnSeconds;
            SpawnNature(Kind.Tree,4);SpawnNature(Kind.Rock,4);
        }
        public static int LootValue(LootKind kind)=>kind==LootKind.SlimeGel||kind==LootKind.Bone?1:kind==LootKind.Club||kind==LootKind.Meat||kind==LootKind.Fur?2:kind==LootKind.Armor||kind==LootKind.Tail?4:kind==LootKind.Staff?5:6;
        public int PendingLootMoney=>Loot.Sum(pair=>LootValue(pair.Key)*pair.Value);
        private void AddLoot(LootKind kind,int count=1){if(!Loot.ContainsKey(kind))Loot[kind]=0;Loot[kind]+=count;}
        private void CollectKillLoot(Body body)
        {
            if(body.Hostile)
            {
                int coins=body.Kind==Kind.Orc?8:body.Kind==Kind.GoblinWarrior?5:body.Kind==Kind.GoblinShaman||body.Kind==Kind.Lizard?4:body.Kind==Kind.Goblin?3:2;
                Money+=coins;EmitText(body,$"+{coins} 골드",FeedbackKind.Normal);
                Sound(SoundCue.Gold,body.Position);
            }
            if(body.Kind==Kind.Slime)AddLoot(LootKind.SlimeGel);
            else if(body.Kind==Kind.Goblin){AddLoot(LootKind.Club);AddLoot(LootKind.Bone);}
            else if(body.Kind==Kind.GoblinWarrior)AddLoot(LootKind.Armor);
            else if(body.Kind==Kind.GoblinShaman)AddLoot(LootKind.Staff);
            else if(body.Kind==Kind.Orc)AddLoot(LootKind.Bracer);
            else if(body.Kind==Kind.Lizard)AddLoot(LootKind.Tail);
            else if(body.Kind==Kind.Animal){AddLoot(LootKind.Meat);AddLoot(LootKind.Fur);}
        }
        private int SellLoot()
        {
            int value=PendingLootMoney;LastLootSummary=string.Join(" · ",Loot.Select(pair=>$"{LootName(pair.Key)} {pair.Value}"));Loot.Clear();return value;
        }
        private void ResetExploration(){Loot.Clear();Relics.Clear();_relicRanks.Clear();_dailyRelicChoices.Clear();DailyRewardPending=DailyRewardChoosing=false;LastLootSummary="";ActiveShrine=ActiveRuins=false;_chestClock=50;_resourceClock=0;_baseTreeLimit=_baseRockLimit=0;RelicsChanged?.Invoke();}
        private void SeedDiscoveries()
        {
            Add(Kind.AbandonedAltar,new Point(-10,5),9999);Add(Kind.Ruins,new Point(9,-11),9999);
            Add(Kind.AbandonedAltar,new Point(-26,-16),9999);Add(Kind.Ruins,new Point(15,24),9999);
            for(int i=0;i<4;i++)SpawnChest();
        }
        private void SpawnChest()
        {
            if(Bodies.Count(b=>b.Alive&&b.Kind==Kind.Chest)>=7)return;
            for(int i=0;i<80;i++)
            {
                var p=new Point(_random.Next(-Settings.MapRadius+3,Settings.MapRadius-2),_random.Next(-Settings.MapRadius+3,Settings.MapRadius-2));
                if(Point.Distance(p,new Point(0,0))<6||!Walkable(p,.65f)||!UnitFree(p,0))continue;
                int roll=_random.Next(100);var grade=roll<60?ChestGrade.Common:roll<90?ChestGrade.Rare:ChestGrade.Ancient;
                var chest=Add(Kind.Chest,p,ChestDurability(grade));chest.ChestGrade=grade;chest.Yaw=(float)_random.NextDouble()*360;return;
            }
        }
        private void ProcessExploration(float dt)
        {
            if(Phase==Phase.Day){_chestClock-=dt;if(_chestClock<=0){_chestClock=45+(float)_random.NextDouble()*35;SpawnChest();}}
        }
        private void WorkChest(Body chest,float effort,FeedbackKind feedback)
        {
            if(!chest.Alive||chest.ChestOpened)return;
            float amount=Math.Min(chest.Health,effort);chest.Health-=amount;
            if(chest.Health>0){EmitText(chest,$"개봉 -{amount:0.#}",feedback);return;}
            chest.ChestOpened=true;int reward=_random.Next(5),tier=(int)chest.ChestGrade,multiplier=1<<tier;string text;
            Sound(SoundCue.Chest,chest.Position);
            if(reward==0){var pool=ChestItems(chest.ChestGrade);var item=pool[_random.Next(pool.Length)];int count=2+tier;AddLoot(item,count);text=$"{LootName(item)} ×{count} (새벽 판매)";}
            else if(reward==1){int amountMoney=25*multiplier;Money+=amountMoney;text=$"골드 +{amountMoney}";}
            else if(reward==2){int amountWood=40*multiplier;Money+=amountWood;text=$"목재 판매 · 골드 +{amountWood}";}
            else if(reward==3){int amountStone=30*multiplier;Money+=amountStone*2;text=$"석재 판매 · 골드 +{amountStone*2}";}
            else
            {
                // 등급 허용 풀과 미보유 목록의 교집합 안에서 직접 추첨한다. 중복 재추첨은 하지 않는다.
                var available=ChestRelics(chest.ChestGrade).Where(k=>!Relics.Contains(k)).ToArray();
                if(available.Length==0){int bonus=35*multiplier;Money+=bonus;text=$"이 등급 유물 모두 보유 · 골드 +{bonus}";}
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
            if(shrine!=ActiveShrine||ruins!=ActiveRuins)Say("마을 편입 효과 · "+(shrine?"제단: 자동 건설 +25% ":"")+(ruins?"유적: 타워 사거리 +1":""));
            ActiveShrine=shrine;ActiveRuins=ruins;
        }
        private float CriticalDamage(float damage)=>damage*(_random.NextDouble()<Math.Min(1,.05+.15*RelicLevel(RelicKind.Critical))?2:1);
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
            var center=new Point(x+(body.Width-1)*.5f,z);
            if(Point.Distance(center,body.Position)<.1f)return "현재 위치입니다.";
            for(int dx=0;dx<body.Width;dx++)
            {
                var p=new Point(x+dx,z);
                if(Terrain.At(p)==TerrainKind.Water||!Territory.Contains(TileKey(x+dx,z)))return "마을 영역의 육지에 놓으세요.";
                if(Bodies.Any(b=>b.Alive&&(b.Building?b.DistanceTo(p)<.49f:Point.Distance(b.Position,p)<.8f)))return "목적지가 다른 오브젝트와 겹칩니다.";
            }return null;
        }
        public void BeginRelocation(int id,int x,int z)
        {
            var body=Find(id);string error=MoveError(body,x,z);if(Frozen||error!=null){if(error!=null){Sound(SoundCue.Error);Say(error);}return;}
            var site=AddBuilding(body.Kind,x,z,false);site.PaidGold=0;site.Health=body.Health;site.MaxHealth=body.MaxHealth;site.MoveSource=id;
            body.MovingOut=true;body.MoveSite=site.Id;body.MoveProgress=0;TopologyRevision++;InvalidatePaths();Say("건물 이전 시작 · 원위치 철거/목적지 재건에 커서 지원 가능");
            Sound(SoundCue.Place);
        }
        private void AdvanceMoves(float dt)
        {foreach(var body in Bodies)if(body.Alive&&body.MovingOut)AdvanceMove(body,AutoBuildMultiplier*dt/body.ConstructionSeconds);}
        private void AdvanceMove(Body body,float progress)
        {
            if(body==null||!body.MovingOut)return;var site=Find(body.MoveSite);if(site==null)return;
            body.MoveProgress=Math.Min(1,body.MoveProgress+progress);body.Construction=1-body.MoveProgress;site.Construction=body.MoveProgress;
            if(body.MoveProgress<1)return;
            site.MoveSource=0;site.TowerLevel=body.TowerLevel;site.Health=Math.Min(site.Health,body.Health);while(body.Orders.Count>0)site.Orders.Enqueue(body.Orders.Dequeue());
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
