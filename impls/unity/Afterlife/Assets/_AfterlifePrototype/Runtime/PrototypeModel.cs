using System;
using System.Collections.Generic;
using System.Linq;

namespace Afterlife.Prototype
{
    // Market 값은 기존 직렬화 번호 보존용. 일반 플레이에서는 생성/건설하지 않는다.
    public enum Phase { Day, Dusk, Night, Settlement, Rebirth, Victory, Defeat }
    public enum InputMode { Work, Build }
    public enum FeedbackKind { Normal, Critical, Fire, Burn, EnemyDamage, Lightning }
    public struct Point
    {
        public float X, Z;
        public Point(float x,float z) { X=x; Z=z; }
        public static float Distance(Point a,Point b) => (float)Math.Sqrt((a.X-b.X)*(a.X-b.X)+(a.Z-b.Z)*(a.Z-b.Z));
        public static Point Move(Point a,Point b,float distance)
        { float d=Distance(a,b); return d<=distance||d<.0001f?b:new Point(a.X+(b.X-a.X)*distance/d,a.Z+(b.Z-a.Z)*distance/d); }
    }
    public sealed class Order { public float Progress; public int Paid; public Kind Kind; public float Seconds; }
    public sealed class VisionEcho
    {
        public Point Position; public float Life;
        public float Strength(float lifetime)
        {
            float age=1-Life/Math.Max(.1f,lifetime);
            float t=Math.Max(0,Math.Min(1,(age-.2f)/.8f));
            return 1-t*t*(3-2*t);
        }
    }
    public sealed class Body
    {
        public int Id, Width=1, Depth=1, Owner, PatrolIndex;
        public bool BlocksMovement=true;
        public Kind Kind;
        public int Variant;public ChestGrade ChestGrade;public bool ChestOpened;
        public Point Position, Rally, Destination;
        public float Health, MaxHealth, Construction=1, ConstructionSeconds, AttackTimer, RouteTimer;
        public int PaidGold,TowerLevel;
        public float BurnRemaining,BurnDps,BurnClock,PatrolWait,StuckTime;
        public float SpiritShield, ShieldRemaining, ShockRemaining, ShockMultiplier;
        public bool GuardianShield;
        public bool ManualGuard,GuardAssigned;public int GuardSlot,CombatTarget;public Point GuardPost,MeleePoint,PathGoal;
        public int PathTargetId;public float PathRange;
        public bool AttackMove, Raider;
        public float StunRemaining, StunUntil, AttackWindup, AttackRecovery;
        public int PendingTarget;
        public Point Facing,FiringPosition;public float FiringSearch;public int FiringTarget;
        public readonly Queue<Order> Orders=new Queue<Order>();
        public readonly Queue<Point> Route=new Queue<Point>();
        // 자원 잔량은 Health 하나만 보관한다. 기존 QA 접근도 같은 값을 사용한다.
        public float Stock {get=>Health;set=>Health=value;}
        public bool Alive=>Health>0;
        public bool Building=>VillageModel.IsBuildingKind(Kind);
        public bool Friendly=>Kind>=Kind.Warrior&&Kind<=Kind.Knight;
        public bool Hostile=>VillageModel.IsHostile(Kind);
        public bool Unit=>Friendly||Hostile||Kind==Kind.Animal;
        public bool Complete=>MovingOut||Construction>=1;
        public bool MovingOut;public int MoveSite,MoveSource;public float MoveProgress,Yaw,FleeUntil;public Point FleeFrom;
        public float DistanceTo(Point p)
        {
            if(!Building) return Math.Max(0,Point.Distance(Position,p)-(Unit?.32f:.3f));
            float dx=Math.Max(0,Math.Abs(p.X-Position.X)-Width*.5f), dz=Math.Max(0,Math.Abs(p.Z-Position.Z)-Depth*.5f);
            return (float)Math.Sqrt(dx*dx+dz*dz);
        }
    }
    // 모델은 판 데이터의 유일한 소유자이며 Unity 오브젝트를 참조하지 않는다.
    public sealed partial class VillageModel
    {
        public readonly Balance Settings;
        public PrototypeRules Rules => Settings.Rules;
        public readonly TerrainMap Terrain;
        public readonly List<Body> Bodies=new List<Body>();
        public readonly HashSet<int> Territory=new HashSet<int>();
        public readonly HashSet<int> Selected=new HashSet<int>();
        public readonly List<VisionEcho> Echoes=new List<VisionEcho>();
        public event Action<string> Notice;
        public event Action<Body> Contributed;
        public event Action<Body> Damaged;
        public event Action<Body,string,FeedbackKind> ContributionText;
        private void EmitText(Body body,string text,FeedbackKind kind=FeedbackKind.Normal)=>ContributionText?.Invoke(body,text,kind);
        public event Action<Point,Kind,int> HarvestGold;
        private double _goldRemainder;
        private int GatherGold(float amount,Kind kind)
        {
            double total=_goldRemainder+amount*(kind==Kind.Rock?Rules.Resources.RockGold:Rules.Resources.TreeGold)*HarvestGoldMultiplier;
            int whole=(int)Math.Floor(total+0.00001);_goldRemainder=Math.Max(0,total-whole);return whole;
        }
        public event Action RunEnded;
        private int _pathBudget, _unitStart;
        public Phase Phase {get;private set;}
        public int Wave {get;private set;}
        public float Remaining {get;private set;}
        public float Money {get;private set;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public bool DebugCursorBoost {get;set;}
        private bool _debugCursorSpeedBoost;
        public bool DebugCursorSpeedBoost
        {
            get=>_debugCursorSpeedBoost;
            set
            {
                if(_debugCursorSpeedBoost==value)return;
                float progress=PulseFraction;
                _debugCursorSpeedBoost=value;
                _pulseClock=progress*Interval;
            }
        }
        public void DebugAddGold() { Money+=100; }
        public bool CanDebugSkipPhase=>Phase==Phase.Day||Phase==Phase.Dusk||Phase==Phase.Night;
        public void DebugSkipPhase()
        {
            if(!CanDebugSkipPhase)return;
            Remaining=0;
            if(Phase==Phase.Day)BeginDusk();
            else if(Phase==Phase.Dusk)BeginNight();
            else
            {
                // Skip combat without awarding kills or drops for unearned victories.
                Bodies.RemoveAll(body=>body.Raider);
                _spawned=WaveEnemyCount;
                BeginSettlement();
            }
        }
        private float DebugCursorMultiplier=>DebugCursorBoost?100:1;
        private float DebugCursorSpeedMultiplier=>DebugCursorSpeedBoost?100:1;
#else
        private float DebugCursorMultiplier=>1;
        private float DebugCursorSpeedMultiplier=>1;
#endif
        public int Population {get;private set;}
        public int Capacity {get;private set;}
        public bool Working {get;private set;}
        public bool PointerVisible {get;private set;}
        public Point WorkCenter {get;private set;}
        public InputMode Mode {get;private set;}
        public Kind BuildKind {get;private set;}
        public int SelectedBarracks {get;private set;}
        public int PulseLevel {get;private set;}
        public int ResidentLevel {get;private set;}
        public int ConstructionLevel {get;private set;}
        public int CriticalChanceLevel {get;private set;}
        public int CriticalPowerLevel {get;private set;}
        public int Valuables {get;private set;}
        public int LastSettlement {get;private set;}
        public int LastValuables {get;private set;}
        public int PulseCount {get;private set;}
        public bool Ended=>Phase==Phase.Victory||Phase==Phase.Defeat;
        public bool Frozen=>Ended||Phase==Phase.Settlement||Phase==Phase.Rebirth;
        public int DaySequence {get;private set;}
        public float Efficiency=>1+Effect("education");
        public float Radius=>Settings.CursorRadius*(1+Rules.Economy.RadiusPerResident*ResidentLevel+Effect("radius")+Effect("housing")+FieldRadiusReward);
        public float Interval=>Math.Max(Rules.Growth.MinimumPulseSeconds,Settings.PulseSeconds*(float)Math.Pow(Rules.Growth.PulseMultiplier,PulseLevel)*Math.Max(.1f,1-Effect("interval")))/DebugCursorSpeedMultiplier;
        public bool CursorAutomationUnlocked=>PulseLevel>0 || Effect("interval")>0;
        public float PulseFraction=>_pulseClock/Interval;
        public float AutoBuildMultiplier=>Settings.AutoConstruction*(1+Rules.Growth.ConstructionPerLevel*ConstructionLevel)*MemoryWorkMultiplier*(ActiveShrine?Rules.Economy.ShrineConstructionMultiplier:1);
        public int ResidentsPerHouse=>0; // Legacy read-only consumers: population is no longer simulated.
        public float NightBlend=>Phase==Phase.Night?1:Phase==Phase.Dusk?1-Remaining/Math.Max(1,Settings.DuskSeconds):0;
        public int TerritoryRevision {get;private set;}
        private int _nextId, _spawned;
        public int TopologyRevision {get;private set;}
        private float _nightTime, _pulseClock, _ecologyClock, _echoClock, _combatTime;
        private Random _random;
        public VillageModel(Balance settings, IEnumerable<ConstructionRule> buildings=null) { Settings=settings; Rules.Validate(); if(buildings!=null)ConfigureConstruction(buildings); Terrain=new TerrainMap(settings.MapRadius); InitializeDivineFields(); Reset(); }
        public static int TileKey(int x,int z)=>(x+128)*256+z+128;
        public static string Name(Kind k)
        {
            switch(k) {
                case Kind.ArrowTower:return "화살 타워";case Kind.MagicTower:return "마법 타워";case Kind.Wall:return "성벽";case Kind.Chest:return "보물 상자";case Kind.AbandonedAltar:return "버려진 제단";case Kind.Ruins:return "고대 유적";case Kind.House:return "주택";case Kind.Farm:return "농장";case Kind.Market:return "교역소";
                case Kind.TownHall:return "마을회관";case Kind.Sanctuary:return "성소";case Kind.ArcheryRange:return "궁수 훈련장";case Kind.KnightHall:return "기사단 본부";case Kind.MageTower:return "마법사 탑";case Kind.Barracks:return "병영";case Kind.Research:return "연구소";case Kind.Altar:return "제단";
                case Kind.Tree:return "나무";case Kind.Rock:return "바위";case Kind.Warrior:return "전사";
                case Kind.Archer:return "궁수";case Kind.Mage:return "마법사";case Kind.Knight:return "기사";
                case Kind.Goblin:return "고블린";case Kind.GoblinWarrior:return "고블린 전사";case Kind.GoblinShaman:return "고블린 주술사";case Kind.Orc:return "오크";case Kind.Lizard:return "리자드";case Kind.Slime:return "슬라임";case Kind.Animal:return "사슴";case Kind.Treasure:return "귀중품";
                default:return "2×1 검증 건물";
            }
        }
        public static int BuildPrice(Kind kind)
        {
            switch(kind)
            {
                case Kind.House:return 20;case Kind.Farm:return 30;case Kind.Barracks:return 80;
                case Kind.ArcheryRange:return 120;case Kind.KnightHall:return 320;case Kind.MageTower:return 240;case Kind.Research:return 110;case Kind.Altar:return 120;case Kind.ArrowTower:return 95;
                case Kind.MagicTower:return 145;case Kind.Wall:return 24;default:return 0;
            }
        }
        public Body Find(int id)=>Bodies.Find(b=>b.Id==id&&b.Alive);
        public bool Has(Kind k)=>Bodies.Any(b=>b.Alive&&b.Complete&&!b.MovingOut&&b.Kind==k);
        public void Reset()
        {
            ClearRunProgress(); LastResult=null;
            ResetAttempt();
            Sound(SoundCue.Day);
        }
        private void ResetAttempt()
        {
            ResetNightCleanup();
            Terrain.Regenerate(Settings.MapSeed);
            InitializeEconomicSites();ResetBuildingSystems();
            DivineFields.Reset();
            ResetManualWork();
            SoundReset?.Invoke();
            ResetProgression();ResetCombatGrowth();ResetExploration();Bodies.Clear();Projectiles.Clear();_obstaclesDirty=true;_nextShotId=0;Territory.Clear();TerritoryRevision++;Selected.Clear();Echoes.Clear();_nextId=0;_random=new Random(Terrain.Seed);
            Money=0;_goldRemainder=0;Population=0;Working=PointerVisible=false;Mode=InputMode.Work;
            ResetFieldSlots();
            SelectedBarracks=0;Wave=1;DaySequence++;Phase=Phase.Day;Remaining=Settings.DayDuration(Wave);
            PulseLevel=ResidentLevel=ConstructionLevel=CriticalChanceLevel=CriticalPowerLevel=Valuables=LastValuables=LastSettlement=PulseCount=0;
            _nightTime=_pulseClock=_ecologyClock=_echoClock=_combatTime=0;_spawned=0; SegmentCleared=SegmentKills=0;
            AddBuilding(Kind.House,0,0,true);
            AddResource(Kind.Tree,-2,0);AddResource(Kind.Tree,-2,1);AddResource(Kind.Rock,-2,-1);AddResource(Kind.Rock,-3,-1);
            AddResource(Kind.Tree,3,2);AddResource(Kind.Tree,4,2);AddResource(Kind.Rock,-4,3);
            SeedDiscoveries();SeedBiomes();
            for(int i=0;i<Rules.Resources.InitialNatureCount;i++)SpawnNature(i%6==0?Kind.Treasure:i%5==0?Kind.Animal:i%3==0?Kind.Rock:Kind.Tree,Rules.Resources.InitialNatureDistance);
            InitializeResourceLimits();Recalculate();TopologyRevision++;Say("좌클릭으로 상호작용 필드를 설치하고 Ctrl/Shift를 눌러 건설합니다.");
        }
        private Body Add(Kind k,Point p,float hp)
        {var b=new Body{Id=++_nextId,Kind=k,Position=p,Rally=p,Destination=p,Health=hp,MaxHealth=hp};Bodies.Add(b);if(!b.Unit)_obstaclesDirty=true;return b;}
        private void AddResource(Kind k,float x,float z)
        {if(!Terrain.Land(new Point(x,z),.35f)||(k==Kind.Tree||k==Kind.Rock)&&Terrain.At(new Point(x,z))==TerrainKind.Dirt)return;var b=Add(k,new Point(x,z),k==Kind.Animal?Rules.Units.Find(k).Health:k==Kind.Tree?Rules.Resources.TreeStock*Wave:k==Kind.Rock?Rules.Resources.RockStock*Wave:1);b.Variant=(b.Id-1)%4;}
        private Body AddBuilding(Kind k,int x,int z,bool complete)
        {
            var stats=Building(k);int w=stats.Width,d=stats.Depth;var b=Add(k,new Point(x+(w-1)*.5f,z+(d-1)*.5f),k==Kind.Wall?WallHealth:stats.Health*(k==Kind.House?1+Rules.Rebirth.HouseHealthPerLevel*MemoryHouse:1));
            b.MaxHealth*=1+Effect("structure");b.Health=b.MaxHealth;
            b.Width=w;b.Depth=d;b.BlocksMovement=stats.BlocksMovement;b.Construction=complete?1:0;b.ConstructionSeconds=stats.ConstructionSeconds;
            b.Rally=new Point(b.Position.X+1.5f,b.Position.Z);b.PaidGold=complete?0:ConstructionPrice(k);return b;
        }
        public void UpdatePointer(Point point,bool reveal,bool contribute)
        {WorkCenter=point;PointerVisible=reveal;Working=contribute&&!Frozen&&Mode==InputMode.Work;}
        public void SetMode(InputMode mode,Kind kind=Kind.House)
        {if(Frozen)return;Mode=mode;BuildKind=kind;Selected.Clear();}
        public void ClearMode() {Mode=InputMode.Work;Selected.Clear();}
        public void Select(IEnumerable<int> ids)
        {Selected.Clear();foreach(int id in ids){var b=Find(id);if(b!=null&&b.Friendly&&Visibility(b.Position)>.15f)Selected.Add(id);}Mode=InputMode.Work;if(Selected.Count>0)Sound(SoundCue.Select);}
        public void SelectBarracks(int id){var b=Find(id);if(b!=null&&IsProductionBuilding(b.Kind)&&b.Complete)SelectedBarracks=id;}
        public string PlacementError(Kind k,int x,int z)
        {
            if(!IsBuildingKind(k))return "건물이 아닙니다.";
            var requirement=ConstructionRequirement(k);if(requirement!=null)return requirement;
            if(k==Kind.Market)return "교역소는 더 이상 사용하지 않습니다.";
            string tileError=BuildTileError(x,z,Building(k).Width,Building(k).Depth,k==Kind.House);
            if(tileError!=null)return tileError;
            return Money<ConstructionPrice(k)?"골드가 부족합니다.":null;
        }
        // 위치의 건설 가능 여부는 골드/개별 건물 해금 조건과 분리한다.
        public string BuildTileError(int x,int z,int width=1,int depth=1,bool allowOutside=false)
        {
            for(int dx=0;dx<width;dx++)for(int dz=0;dz<depth;dz++)
            {
                var p=new Point(x+dx,z+dz);
                if(Terrain.At(p)==TerrainKind.Water)return "물 타일에는 건설할 수 없습니다.";
                if(!allowOutside&&!Territory.Contains(TileKey(x+dx,z+dz)))return "모든 점유 타일이 마을 영역 안이어야 합니다.";
                if(Bodies.Any(b=>b.Alive&&(b.Building?b.DistanceTo(p)<.49f:Point.Distance(b.Position,p)<.8f)))return "다른 오브젝트와 겹칩니다.";
            }
            return null;
        }
        public void Place(Kind k,int x,int z)
        {
            if(Frozen)return;string error=PlacementError(k,x,z);if(error!=null){Sound(SoundCue.Error);Say(error);return;}
            Money-=ConstructionPrice(k);AddBuilding(k,x,z,false);
            Sound(SoundCue.Place);
            TopologyRevision++;InvalidatePaths();Say(Name(k)+" 자동 공사 시작");
        }
        public void CancelConstruction(int id)
        {
            var b=Find(id);if(b==null||!b.Building||b.Complete||Frozen)return;
            Money+=(float)Math.Floor(b.PaidGold*(1-b.Construction));b.Health=0;
            Sound(SoundCue.Cancel);
            Recalculate();InvalidatePaths();Say("공사 취소 · 미진행 비율 환급");
        }
        public int Price(Kind kind)=>kind==Kind.Warrior?Settings.WarriorMoney:Rules.Units.Find(kind).Price;
        public const int MaxQueuedOrders=10;
        public float RecruitmentSeconds(Kind kind)=>Settings.WarriorSeconds*Rules.Units.Find(kind).RecruitmentMultiplier/MemoryArmyMultiplier;
        public float TroopHealth(Kind kind)=>Rules.Units.Find(kind).Health;
        public float TroopDamage(Kind kind)=>Damage(kind);
        public float TroopRange(Kind kind)=>AttackRange(kind);
        public bool Unlocked(Kind kind)=>!Rules.Units.Find(kind).RequiresBuilding||(kind==Kind.Knight||Rules.Units.Find(kind).RequiredBuilding==Kind.Altar?HasAltar:Has(Rules.Units.Find(kind).RequiredBuilding));
        public void Recruit(int id,Kind kind)
        {
            // Kept for legacy consumers: buildings now replenish automatically.
            var body=Find(id);if(body!=null&&IsProductionBuilding(body.Kind))SelectedBarracks=id;
        }
        public void CancelOrder(int id)
        {var b=Find(id);if(Frozen||b==null||b.Orders.Count==0)return;var o=b.Orders.Dequeue();Money+=(float)Math.Floor(o.Paid*(1-o.Progress));Sound(SoundCue.Cancel);}
        public void CancelQueuedOrder(int id,Order target)
        {
            var b=Find(id);
            if(Frozen||b==null||!b.Alive||b.Kind!=Kind.Barracks||target==null||b.Orders.Count<2)return;
            // 화면 갱신 사이에 생산이 완료되어도 다른 예약이나 현재 생산을 취소하지 않는다.
            if(ReferenceEquals(b.Orders.Peek(),target)||!b.Orders.Contains(target))return;
            int count=b.Orders.Count;
            for(int i=0;i<count;i++)
            {
                var order=b.Orders.Dequeue();
                if(!ReferenceEquals(order,target))b.Orders.Enqueue(order);
            }
            Money+=target.Paid;Sound(SoundCue.Cancel);
        }
        public int Level(Upgrade u)=>u==Upgrade.Pulse?PulseLevel:u==Upgrade.Residents?ResidentLevel:u==Upgrade.Construction?ConstructionLevel:u==Upgrade.CriticalChance?CriticalChanceLevel:u==Upgrade.CriticalPower?CriticalPowerLevel:ExtraLevel(u);
        public int UpgradeCost(Upgrade u)=>(int)Math.Ceiling(Rules.Growth.CostPerLevel*(Level(u)+(u==Upgrade.MultiTarget?0:1))/30f);
        public void BuyUpgrade(Upgrade u)
        {
            if(Frozen||!HasAltar||!UpgradeUnlocked(u)||Level(u)>=UpgradeMax(u)||!TrySpendEtheron(UpgradeCost(u)))return;
            if(u==Upgrade.Pulse)PulseLevel++;else if(u==Upgrade.Residents)ResidentLevel++;else if(u==Upgrade.Construction)ConstructionLevel++;else if(u==Upgrade.CriticalChance)CriticalChanceLevel++;else if(u==Upgrade.CriticalPower)CriticalPowerLevel++;else _combatUpgrades[u]=Level(u)+1;
            if(u==Upgrade.WallHealth)UpgradeWallHealth();
            Recalculate();_pulseClock=Math.Min(_pulseClock,Interval);Say("일반 강화 적용 · 환생/판 종료 때 초기화");
        }
        public void AttackMove(Point destination)
        {
            if(Frozen||!Walkable(destination,.28f)){Sound(SoundCue.Error);Say("이동 가능한 지점에 명령을 놓으세요.");return;}
            var selected=Selected.Select(Find).Where(b=>b!=null&&b.Friendly).ToArray();
            for(int i=0;i<selected.Length;i++)
            {
                var offset=new Point(destination.X+(i%3-1)*.7f,destination.Z+(i/3)*.7f);
                if(!Walkable(offset,.28f))offset=destination;
                selected[i].Destination=offset;selected[i].GuardPost=offset;selected[i].ManualGuard=selected[i].GuardAssigned=true;selected[i].CombatTarget=selected[i].PendingTarget=0;selected[i].FiringTarget=0;selected[i].AttackMove=true;selected[i].Route.Clear();selected[i].RouteTimer=0;
            }
            if(selected.Length>0)Sound(SoundCue.Select);
            Say($"병력 {selected.Length}명 공격 이동 · 도착 지점 방어 유지");
        }
        public void Tick(float dt)
        {
            if(Frozen||dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
            float productionElapsed=dt;dt=Math.Min(dt,.05f);
            if(_manualWorkElapsed<float.MaxValue)_manualWorkElapsed+=dt;
            _combatTime+=dt;TickProgression(dt);_pathBudget=3; UpdateEchoes(dt);ProcessConstruction(dt);
            UpdateDivineFieldTargets();
            if(Settings.MvpSlots)FieldSlots.Tick(dt);
            DivineFields.Tick(dt,Interval);
            if(MergeBuildings())RebuildTowns();TickBuildingProduction(productionElapsed);UpdateTactics(dt);ProcessUnits(dt);ProcessTowers(dt);ProcessProjectiles(dt);ProcessBurns(dt);ProcessExploration(dt);ProcessResourceRegrowth(dt*ProductionSpeed);Cleanup();
            if(!Bodies.Any(b=>b.Alive&&IsHousing(b.Kind))){End(Phase.Defeat);return;}
            Remaining=Math.Max(0,Remaining-dt);
            TickNightCleanup(dt);
            if(Phase==Phase.Day)
            {
                _ecologyClock+=dt;if(_ecologyClock>=Rules.Resources.EcologySeconds){_ecologyClock=0;SpawnNature(Kind.Animal,Rules.Resources.AnimalMinimumDistance);SpawnNature(Kind.Slime,Rules.Resources.SlimeMinimumDistance);}
                if(Remaining<=0)BeginDusk();
            }
            else if(Phase==Phase.Dusk&&Remaining<=0)BeginNight();
            else if(Phase==Phase.Night)
            {
                _nightTime+=dt;int total=WaveEnemyCount;
                int groups=Rules.Waves.At(Wave).Groups,groupSize=(int)Math.Ceiling(total/(float)groups);
                if(_spawned<total&&_nightTime>=(_spawned/groupSize)*Rules.Waves.At(Wave).GroupInterval&&SpawnRaider())_spawned++;
                if(Remaining<=0&&_spawned==total&&!Bodies.Any(b=>b.Alive&&b.Raider))BeginSettlement();
            }
        }
        private void BeginDusk()
        {
            Phase=Phase.Dusk;Remaining=Settings.DuskSeconds;
            Sound(SoundCue.Warning);Say("황혼 · 집에 불이 켜집니다. 어둠 속 침공을 준비하세요.");
        }
        private void BeginNight()
        {
            ResetNightCleanup();
            Phase=Phase.Night;Remaining=Settings.NightSeconds;
            GrantShields(Effect("shield"),0,true);_nightTime=0;_spawned=0;
            Sound(SoundCue.Warning);Say($"{Wave}일 밤 · 웨이브 {Wave} 시작! {WaveEnemyCount}마리 · {WaveThreat}");
        }
        private void End(Phase p)
        {
            ResetNightCleanup();
            LastResult=new RunResult{Victory=p==Phase.Victory,BestCleared=RunBestCleared,Rebirths=RebirthCount,Kills=RunKills};
            if(RunBestCleared>=Rules.Rebirth.BannerUnlockDay)BannerUnlocked=true;
            DivineFields.Reset();ResetManualWork();ClearRunProgress();ResetProgression();ResetCombatGrowth();ResetExploration();ResetFieldSlots();ResetBuildingSystems(); Money=0;_goldRemainder=0;PulseLevel=ResidentLevel=ConstructionLevel=CriticalChanceLevel=CriticalPowerLevel=0;
            Valuables=LastValuables=LastSettlement=0;Bodies.Clear();Projectiles.Clear();_obstaclesDirty=true;Territory.Clear();TerritoryRevision++;Selected.Clear();Echoes.Clear();Population=Capacity=0;
            Phase=p;Working=PointerVisible=false;Mode=InputMode.Work;Remaining=0;Wave=1;SegmentCleared=SegmentKills=0;
            Say(p==Phase.Victory?$"{Settings.Days}번의 밤을 지켰습니다. 판 종료 · 영구 해금만 유지됩니다.":"마지막 집이 파괴되었습니다. 판 종료 · 영구 해금만 유지됩니다.");
            RunEnded?.Invoke();
            Sound(SoundCue.Result);
        }
        private void BeginSettlement()
        {
            ResetNightCleanup();
            ClearNightShields();Projectiles.Clear();SegmentCleared=Wave;RunBestCleared=Math.Max(RunBestCleared,Wave);
            if(Settings.MvpSlots && Wave>=Settings.Days){End(Phase.Victory);return;}
            Phase=Phase.Settlement;Working=false;LastValuables=Valuables;LastSettlement=Valuables*Rules.Resources.ValuableGold+SellLoot();Money+=LastSettlement;Valuables=0;PrepareDailyRelics();
            Sound(SoundCue.Day);if(LastSettlement>0)Sound(SoundCue.Gold);
            Say($"{Wave}일 웨이브 클리어 · 새벽 정산 · 귀중품 {LastValuables}개 → 골드 {LastSettlement}." + (Settings.MvpSlots ? "" : " 에테론 지급 없음."));
        }
        public void CompleteSettlement()
        {if(Phase!=Phase.Settlement||DailyRewardPending)return;if(Wave>=Settings.Days){End(Phase.Victory);return;}Wave++;DaySequence++;Phase=Phase.Day;Remaining=Settings.DayDuration(Wave);_ecologyClock=0;Sound(SoundCue.Day);}
        private void UpdateEchoes(float dt)
        {
            for(int i=Echoes.Count-1;i>=0;i--){Echoes[i].Life-=dt;if(Echoes[i].Life<=0)Echoes.RemoveAt(i);}
            _echoClock+=dt;
            if(!PointerVisible)return;
            float lifetime=Math.Max(.1f,Settings.VisionTrailSeconds);
            Point previous=Echoes.Count>0?Echoes[Echoes.Count-1].Position:WorkCenter;
            float distance=Point.Distance(previous,WorkCenter);
            if(Echoes.Count>0 && distance<.12f){Echoes[Echoes.Count-1].Life=lifetime;return;}
            if(Echoes.Count>0 && _echoClock<.06f && distance<.35f)return;
            int steps=distance<Settings.CursorVision*2?Math.Max(1,Math.Min(8,(int)Math.Ceiling(distance/Math.Max(.25f,Settings.CursorVision*.35f)))):1;
            for(int i=1;i<=steps;i++)
            {
                float t=i/(float)steps;
                Echoes.Add(new VisionEcho{Position=new Point(previous.X+(WorkCenter.X-previous.X)*t,previous.Z+(WorkCenter.Z-previous.Z)*t),Life=lifetime});
                if(Echoes.Count>32)Echoes.RemoveAt(0);
            }
            _echoClock=0;
        }
        private static float Clamp01(float n)=>Math.Max(0,Math.Min(1,n));
        private float SightStrength(Point p,Point center,float radius)
        {
            return Afterlife.Buildings.CircularRange.SightStrength(p.X,p.Z,center.X,center.Z,radius);
        }
        public float Visibility(Point p)
        {
            float amount=TerritorySight.Contains(p.X,p.Z)?1:0;
            foreach(var b in Bodies)
            {
                float radius=BodyVisionRadius(b);if(radius<=0)continue;
                float strength=SightStrength(p,b.Position,radius);
                amount=Math.Max(amount,strength);
            }
            if(PointerVisible)amount=Math.Max(amount,SightStrength(p,WorkCenter,Settings.CursorVision));
            foreach(var field in DivineFields.Fields)amount=Math.Max(amount,SightStrength(p,new Point(field.X,field.Z),Math.Max(1.6f,field.Radius+1f)));
            foreach(var echo in Echoes)amount=Math.Max(amount,SightStrength(p,echo.Position,Settings.CursorVision)*echo.Strength(Settings.VisionTrailSeconds));
            return amount;
        }
        private void ProcessConstruction(float dt)
        {
            bool changed=false;
            foreach(var b in Bodies)if(b.Alive&&b.Building&&!b.Complete&&b.MoveSource==0)
            {b.Construction=Math.Min(1,b.Construction+AutoBuildMultiplier*LocalConstructionMultiplier(b)*dt/(b.ConstructionSeconds*ConstructionWorkFactor));if(b.Complete){changed=true;Sound(SoundCue.Complete);if(b.Kind==Kind.Barracks)SelectedBarracks=b.Id;}}
            AdvanceMoves(dt);if(changed){TopologyRevision++;Recalculate();}
        }
        private void Pulse()
        {
            bool changed=false;foreach(var b in Bodies)if(b.Alive&&b.DistanceTo(WorkCenter)<=Radius)ContributeBody(b,ref changed);
            if(changed){TopologyRevision++;Recalculate();}
        }
        private void ContributeBody(Body b,ref bool changed)
        {
            if(!b.Alive)return;
            bool work=b.MovingOut||b.MoveSource!=0||b.Kind==Kind.Chest||b.Kind==Kind.Tree||b.Kind==Kind.Rock||b.Hostile||b.Kind==Kind.Animal||b.Building&&(!b.Complete||b.Health<b.MaxHealth&&Money>0);
            if(!work&&b.Kind!=Kind.Treasure)return;
            bool critical=false;float power=work?RollContribution(out critical):1;FeedbackKind feedback=critical?FeedbackKind.Critical:FeedbackKind.Normal;
            power*=DebugCursorMultiplier;
            if(critical)Sound(SoundCue.Critical,b.Position);
            float effort=Efficiency*MemoryWorkMultiplier*Settings.ContributionPower*power*WorkMultiplier*CooperationMultiplier();
            if(b.MovingOut||b.MoveSource!=0)
            {Contributed?.Invoke(b);Sound(SoundCue.Work,b.Position);var source=b.MoveSource!=0?Find(b.MoveSource):b;var site=source!=null?Find(source.MoveSite):null;AdvanceMove(source,effort*LocalConstructionMultiplier(site??b)/(b.ConstructionSeconds*ConstructionWorkFactor));EmitText(b,"이전 작업 지원",feedback);}
            else if(b.Kind==Kind.Chest){Contributed?.Invoke(b);WorkChest(b,Settings.WoodRate*effort,feedback);}
            else if(b.Kind==Kind.Tree||b.Kind==Kind.Rock)
            {
                Contributed?.Invoke(b);
                Sound(b.Kind==Kind.Tree?SoundCue.Wood:SoundCue.Stone,b.Position);
                float bonus=1+(b.Kind==Kind.Tree?Rules.Relics.Wood*RelicLevel(RelicKind.Wood):Rules.Relics.Stone*RelicLevel(RelicKind.Stone));
                float amount=Math.Min(b.Health,(b.Kind==Kind.Tree?Settings.WoodRate:Settings.StoneRate)*effort*bonus*ProductionSpeed);
                b.Health=Math.Max(0,b.Health-amount);
                int gained=GatherGold(amount,b.Kind);Money+=gained;if(gained>0)HarvestGold?.Invoke(b.Position,b.Kind,gained);if(amount>0)EmitText(b,$"채집 {amount:0.#}",feedback);
                if(b.Health<=.001f){b.Health=0;Sound(b.Kind==Kind.Tree?SoundCue.TreeFall:SoundCue.StoneBreak,b.Position);if(_random.NextDouble()<Rules.Resources.ValuableChance)Valuables++;}
            }
            else if(b.Kind==Kind.Treasure){Contributed?.Invoke(b);Sound(SoundCue.Gold,b.Position);Valuables++;EmitText(b,"귀중품 +1");b.Health=0;Say("귀중품 발견 · 다음 날 정산에서 골드으로 전환됩니다.");}
            else if(b.Building&&!b.Complete)
            {Contributed?.Invoke(b);Sound(SoundCue.Work,b.Position);float before=b.Construction;b.Construction=Math.Min(1,b.Construction+effort*LocalConstructionMultiplier(b)/(b.ConstructionSeconds*ConstructionWorkFactor));EmitText(b,$"건설 +{(b.Construction-before)*100:0.#}%",feedback);changed|=b.Complete;if(b.Complete)Sound(SoundCue.Complete);}
            else if(b.Building&&b.Health<b.MaxHealth)
            {if(Money>0)Contributed?.Invoke(b);float amount=Math.Min(b.MaxHealth-b.Health,Math.Min(Settings.RepairRate*effort*(1+Rules.Relics.Repair*RelicLevel(RelicKind.Repair)),Money*Rules.Economy.RepairPerGold));Money-=amount/Rules.Economy.RepairPerGold;b.Health+=amount;if(amount>0){Sound(SoundCue.Work,b.Position);EmitText(b,$"수리 +{amount:0.#}",feedback);}}
            else if(b.Hostile||b.Kind==Kind.Animal){Contributed?.Invoke(b);if(!critical)Sound(SoundCue.Hit,b.Position);float damage=Settings.CursorDamage*Efficiency*Settings.ContributionPower*power*(1+Effect("physical"));EmitText(b,$"{damage:0.#}",feedback);PlayerHit(b,damage);}
        }
        private float AttackRange(Kind k)=>Rules.Units.Find(k).Range;
        private float Damage(Kind k)=>Rules.Units.Find(k).Damage;
        private void ProcessUnits(float dt)
        {
            var units=Bodies.Where(b=>b.Alive&&b.Unit).ToArray();
            if(units.Length>0)_unitStart=(_unitStart+1)%units.Length;
            for(int unitIndex=0;unitIndex<units.Length;unitIndex++)
            {
                var u=units[(unitIndex+_unitStart)%units.Length];
                if(!u.Alive)continue;
                u.AttackRecovery=Math.Max(0,u.AttackRecovery-dt);
                u.StunRemaining=Math.Max(0,u.StunUntil-_combatTime);
                if(u.StunRemaining>0){u.PendingTarget=0;u.AttackWindup=u.AttackRecovery=0;continue;}
                if(u.Kind==Kind.Animal&&u.FleeUntil>_combatTime){Flee(u,dt);continue;}
                u.AttackTimer-=dt;
                if(u.PendingTarget!=0)
                {
                    var victim=Find(u.PendingTarget);u.AttackWindup-=dt;
                    if(victim!=null)u.Facing=new Point(victim.Position.X-u.Position.X,victim.Position.Z-u.Position.Z);
                    if(u.AttackWindup<=0)
                    {
                        if(victim!=null&&victim.DistanceTo(u.Position)<=AttackRange(u.Kind)+.12f&&ClearAttack(u.Position,victim,u.Id))ResolveAttack(u,victim);
                        u.PendingTarget=0;u.AttackRecovery=Rules.Combat.AttackRecovery;u.AttackTimer=Rules.Units.Find(u.Kind).AttackSeconds;
                    }
                    continue;
                }
                Body target=null;
                if(u.Friendly)
                {
                    target=FriendlyTarget(u);
                }
                else if(u.Hostile)
                {
                    target=EnemyTarget(u);
                }
                if(target!=null)
                {
                    float range=AttackRange(u.Kind);
                    if(target.DistanceTo(u.Position)<=range&&ClearAttack(u.Position,target,u.Id))
                    {u.Route.Clear();u.Facing=new Point(target.Position.X-u.Position.X,target.Position.Z-u.Position.Z);if(u.AttackTimer<=0){u.PendingTarget=target.Id;u.AttackWindup=Rules.Combat.AttackWindup;}}
                    else if(IsRanged(u.Kind))SeekFiringPosition(u,target,range,dt);else if(u.Friendly)Navigate(u,u.MeleePoint,null,.2f,dt);else Navigate(u,target.Position,target,range,dt);
                }
                else if(u.Friendly&&u.AttackMove)
                {if(Point.Distance(u.Position,u.Destination)<.65f){u.AttackMove=false;u.Route.Clear();u.RouteTimer=0;u.Destination=u.GuardPost;}else Navigate(u,u.Destination,null,.25f,dt);}
                else if(u.Friendly)GuardPatrol(u,dt);
                else if(u.Raider)
                {
                    // 시야 밖에서는 공격 대상을 지정하지 않고 마을로 접근한다.
                    var village=Bodies.Where(b=>b.Alive&&IsHousing(b.Kind)).OrderBy(b=>b.DistanceTo(u.Position)).FirstOrDefault();
                    if(village!=null)Navigate(u,village.Position,village,.8f,dt);else Patrol(u,dt);
                }
                else Patrol(u,dt);
            }
        }
        private void Patrol(Body u,float dt)
        {
            if(Point.Distance(u.Position,u.Destination)<.7f||!Walkable(u.Destination,.34f)||u.Route.Count==0&&u.RouteTimer<=0)
            {
                Point center=u.Rally;
                for(int i=0;i<12;i++)
                {
                    float a=(float)(_random.NextDouble()*Math.PI*2),r=2;
                    var p=new Point(center.X+(float)Math.Cos(a)*r,center.Z+(float)Math.Sin(a)*r);
                    if(Walkable(p,.34f)){u.Destination=p;u.Route.Clear();u.RouteTimer=0;break;}
                }
            }
            Navigate(u,u.Destination,null,.3f,dt);
        }
        private bool ClearAttack(Point p,Body target,int sourceId=0)=>SightClear(p,target.Position,sourceId,target.Id,.65f,.65f);
        public bool Walkable(Point p,float radius)
        {
            if(Math.Abs(p.X)>=Settings.MapRadius||Math.Abs(p.Z)>=Settings.MapRadius||!Terrain.Land(p,radius))return false;
            return StaticFree(p,radius);
        }
        private bool UnitFree(Point p,int self)
        {foreach(var b in Bodies)if(b.Id!=self&&b.Alive&&b.Unit&&Point.Distance(p,b.Position)<.68f)return false;return true;}
        private void Navigate(Body u,Point goal,Body target,float range,float dt)
        {
            if(Point.Distance(goal,u.PathGoal)>.65f||u.PathTargetId!=(target?.Id??0)||u.PathRange!=range)
            {u.PathGoal=goal;u.PathTargetId=target?.Id??0;u.PathRange=range;u.Route.Clear();u.RouteTimer=0;}
            u.RouteTimer-=dt;
            // 진행 중인 경로는 유지한다. 목표 변경, 정체, 지형 변경 시 기존 무효화 처리가 재탐색한다.
            if(u.Route.Count==0&&u.RouteTimer<=0&&_pathBudget>0)
            {
                _pathBudget--; u.RouteTimer=1.6f+(u.Id%7)*.09f;u.PathGoal=goal;
                foreach(var p in Pathfinder.Find(u.Position,goal,target,range,this,u.Id))u.Route.Enqueue(p);
            }
            if(u.Route.Count==0)return;
            float travel=MoveSpeed(u.Kind)*(u.Kind==Kind.Animal&&u.FleeUntil>_combatTime?Rules.Combat.AnimalFleeMultiplier:1)*dt;
            Point next=Point.Move(u.Position,u.Route.Peek(),travel);
            Point before=u.Position;
            if(Walkable(next,.34f)&&UnitFree(next,u.Id))u.Position=next;
            else
            {
                // 겹침 대신 옆으로 비켜간다. 순차 적용으로 앞선 유닛의 최신 위치도 검사한다.
                float angle=(float)Math.Atan2(next.Z-u.Position.Z,next.X-u.Position.X);
                for(int i=0;i<10;i++){float side=u.Id%2==0?1:-1;float a=angle+side*(i%2==0?1:-1)*(i/2+1)*.52f;var p=new Point(u.Position.X+(float)Math.Cos(a)*travel,u.Position.Z+(float)Math.Sin(a)*travel);if(Walkable(p,.34f)&&UnitFree(p,u.Id)){u.Position=p;break;}}
            }
            // 옆걸음만 반복해도 정체로 계산해 혼잡 우회 경로를 다시 찾는다.
            float progress=Point.Distance(before,u.Route.Peek())-Point.Distance(u.Position,u.Route.Peek());
            if(progress<travel*.2f){u.StuckTime+=dt;if(u.StuckTime>.4f){u.Route.Clear();u.RouteTimer=0;u.StuckTime=0;}}else u.StuckTime=Math.Max(0,u.StuckTime-dt);
            if(u.Route.Count>0&&Point.Distance(u.Position,u.Route.Peek())<.04f)u.Route.Dequeue();
        }
        private void SpawnNature(Kind k,float minimum)
        {
            int limit=k==Kind.Tree||k==Kind.Rock?ResourceLimit(k):IsHostile(k)?8:k==Kind.Animal?12:110;
            if(Bodies.Count(b=>b.Alive&&b.Kind==k)>=limit)return;
            for(int i=0;i<50;i++)
            {
                var p=new Point(_random.Next(-Settings.MapRadius+2,Settings.MapRadius-1),_random.Next(-Settings.MapRadius+2,Settings.MapRadius-1));
                if((k==Kind.Tree||k==Kind.Rock)&&!InResourceCluster(k,p))continue;
                if(Point.Distance(p,new Point(0,0))<minimum||!Walkable(p,.7f)||!UnitFree(p,0))continue;
                if(Bodies.Any(b=>b.Alive&&Point.Distance(p,b.Position)<1.1f))continue;
                if(IsHostile(k))Add(k,p,EnemyHealth(k));else AddResource(k,p.X,p.Z);return;
            }
        }
        private bool SpawnRaider()
        {
            // 현재 공유 시야와 잔상을 포함한 암흑에서만 생성. 위치를 찾지 못하면 다음 틱에 재시도한다.
            for(int i=0;i<80;i++)
            {
                int fronts=Rules.Waves.At(Wave).Fronts;
                double front=(Wave*.9+(_spawned%fronts)*Math.PI*2/fronts);
                double angle=i<50?front+(_random.NextDouble()-.5)*.65:_random.NextDouble()*Math.PI*2;
                float extent=Bodies.Where(h=>h.Alive&&IsHousing(h.Kind)&&h.Complete).Select(h=>Point.Distance(h.Position,new Point(0,0))+Math.Max(h.Width,h.Depth)*.5f).DefaultIfEmpty(0).Max();
                float r=Math.Min(Settings.MapRadius-3,extent+Rules.Waves.SpawnDistance+(float)_random.NextDouble()*Rules.Waves.SpawnDistanceVariation);
                var p=new Point((float)Math.Cos(angle)*r,(float)Math.Sin(angle)*r);
                if(i>=60)p=new Point(_random.Next(-Settings.MapRadius+2,Settings.MapRadius-1),_random.Next(-Settings.MapRadius+2,Settings.MapRadius-1));
                if((!NightOvertime&&Visibility(p)>.015f)||!Walkable(p,.4f)||!UnitFree(p,0))continue;
                Kind kind=WaveEnemy(_spawned);var b=Add(kind,p,EnemyHealth(kind)*(1+Rules.Waves.HealthGrowth*(Wave-1)*(Wave-1)));b.Raider=true;return true;
            }
            return false;
        }
        private void Cleanup()
        {
            bool buildings=false;
            foreach(var b in Bodies.Where(b=>!b.Alive).ToArray())
            {AbortDestroyedMove(b);buildings|=b.Building;foreach(var o in b.Orders)Money+=(float)Math.Floor(o.Paid*(1-o.Progress));Selected.Remove(b.Id);Bodies.Remove(b);_production.Remove(b.Id);if(!b.Unit)_obstaclesDirty=true;}
            if(buildings){TopologyRevision++;Recalculate();InvalidatePaths();}
        }
        private void Recalculate() => RebuildTowns();
        private void InvalidatePaths(){foreach(var b in Bodies){b.Route.Clear();b.RouteTimer=0;}}
        private static float Clamp(float n,float min,float max)=>Math.Max(min,Math.Min(max,n));
        private void Say(string text)=>Notice?.Invoke(text);
    }
    internal sealed class SearchHeap
    {
        private readonly List<(int Key,float Score)> _items=new List<(int,float)>();
        private readonly Dictionary<int,int> _indices=new Dictionary<int,int>();
        public int Count=>_items.Count;
        public void Push(int key,float score)
        {
            // 이미 대기 중인 칸은 우선순위만 낮춰 탐색 횟수를 중복 항목에 쓰지 않는다.
            if(_indices.TryGetValue(key,out int i))
            {if(_items[i].Score<=score)return;}
            else{i=_items.Count;_items.Add((key,score));}
            while(i>0)
            {
                int parent=(i-1)/2;if(_items[parent].Score<=score)break;
                _items[i]=_items[parent];_indices[_items[i].Key]=i;i=parent;
            }
            _items[i]=(key,score);_indices[key]=i;
        }
        public int Pop()
        {
            int key=_items[0].Key;var last=_items[_items.Count-1];_items.RemoveAt(_items.Count-1);_indices.Remove(key);
            if(_items.Count==0)return key;
            int i=0;
            while(i*2+1<_items.Count)
            {
                int child=i*2+1;if(child+1<_items.Count&&_items[child+1].Score<_items[child].Score)child++;
                if(_items[child].Score>=last.Score)break;
                _items[i]=_items[child];_indices[_items[i].Key]=i;i=child;
            }
            _items[i]=last;_indices[last.Key]=i;return key;
        }
    }
    internal static class Pathfinder
    {
        private static Point World(int key,int width,int radius)=>new Point((key%width-radius)*.5f,(key/width-radius)*.5f);
        public static IEnumerable<Point> Find(Point start,Point goal,Body target,float range,VillageModel model,int self)
        {
            // 검색 시점 유닛 위치의 스냅샷. 점유는 통행 불가가 아닌 비용으로 두어 좁은 길을 봉쇄하지 않는다.
            var occupied=model.Bodies.Where(b=>b.Alive&&b.Unit&&b.Id!=self&&b.Id!=(target?.Id??0)).Select(b=>b.Position).ToArray();
            float Crowd(Point point)
            {
                float cost=0;foreach(var unit in occupied){float dx=point.X-unit.X,dz=point.Z-unit.Z,squared=dx*dx+dz*dz;if(squared<1.5625f)cost+=squared<.4624f?8:1.5f*(1.25f-(float)Math.Sqrt(squared))/.57f;}
                return cost;
            }
            // 열린 구간은 연속 좌표 직선 경로. 막힌 경우에만 A* 표본 경로를 구한다.
            Point end=goal;
            if(target!=null)
            {
                Point surface=goal;
                if(target.Building)surface=new Point(Math.Max(goal.X-target.Width*.5f,Math.Min(goal.X+target.Width*.5f,start.X)),Math.Max(goal.Z-target.Depth*.5f,Math.Min(goal.Z+target.Depth*.5f,start.Z)));
                end=Point.Move(surface,start,range*.8f+(target.Building?0:.32f));
            }
            bool straight=true;int steps=Math.Max(1,(int)(Point.Distance(start,end)*5));
            for(int i=1;i<=steps;i++){var p=new Point(start.X+(end.X-start.X)*i/steps,start.Z+(end.Z-start.Z)*i/steps);if(!model.Walkable(p,.34f)||Crowd(p)>.05f){straight=false;break;}}
            if(straight)return new[]{end};
            int radius=model.Settings.MapRadius*2,width=radius*2+1;
            // 직선 표본은 한 번만 검사한다. A*가 재방문하는 격자만 정수 키로 저장한다.
            var walkCache=new Dictionary<int,bool>();var crowdCache=new Dictionary<int,float>();
            bool Free(int key){if(!walkCache.TryGetValue(key,out bool free)){free=model.Walkable(World(key,width,radius),.34f);walkCache[key]=free;}return free;}
            float GridCrowd(int key){if(!crowdCache.TryGetValue(key,out float value)){value=Crowd(World(key,width,radius));crowdCache[key]=value;}return value;}
            int initial=((int)Math.Round(start.Z*2)+radius)*width+(int)Math.Round(start.X*2)+radius;
            var previous=new Dictionary<int,int>{{initial,-1}};var cost=new Dictionary<int,float>{{initial,0}};
            var open=new SearchHeap();open.Push(initial,Point.Distance(start,goal));var closed=new HashSet<int>();int found=-1,closest=initial;float closestDistance=Point.Distance(start,goal);
            for(int iteration=0;open.Count>0&&iteration<650;iteration++)
            {
                int current=open.Pop();if(!closed.Add(current))continue;Point p=World(current,width,radius);
                float distance=target==null?Point.Distance(p,goal):target.DistanceTo(p);
                if(distance<closestDistance&&current!=initial){closestDistance=distance;closest=current;}
                if((target==null?distance<.45f:distance<=range)&&Free(current)&&GridCrowd(current)<8){found=current;break;}
                int x=current%width,z=current/width;
                for(int dx=-1;dx<=1;dx++)for(int dz=-1;dz<=1;dz++)
                {
                    if(dx==0&&dz==0||x+dx<0||x+dx>=width||z+dz<0||z+dz>=width)continue;
                    int key=(z+dz)*width+x+dx;if(closed.Contains(key))continue;var q=World(key,width,radius);
                    if(!Free(key)||dx!=0&&dz!=0&&(!Free(current+dz*width)||!Free(current+dx)))continue;
                    float c=cost[current]+(dx!=0&&dz!=0?.7071f:.5f)+GridCrowd(key);
                    if(cost.TryGetValue(key,out float old)&&old<=c)continue;cost[key]=c;previous[key]=current;open.Push(key,c+Point.Distance(q,goal));
                }
            }
            if(found<0)found=closest;if(found==initial)return Array.Empty<Point>();var path=new List<Point>();while(found!=initial&&found>=0){path.Add(World(found,width,radius));found=previous[found];}path.Reverse();
            if(target==null&&path.Count>0&&Point.Distance(path[path.Count-1],goal)<.75f&&model.Walkable(goal,.34f)&&Crowd(goal)<8)
            {
                var last=path[path.Count-1];bool connect=true;
                for(int i=1;i<=4;i++){var p=new Point(last.X+(goal.X-last.X)*i/4,last.Z+(goal.Z-last.Z)*i/4);if(!model.Walkable(p,.34f)||Crowd(p)>=8){connect=false;break;}}
                if(connect)path.Add(goal);
            }
            return path;
        }
    }
}
