using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
namespace Afterlife.Prototype
{
    // 기존 GUID와 API를 유지하는 조립 에셋. 이전 값은 에디터에서 한 번만 자식 에셋으로 옮긴다.
    [CreateAssetMenu(menuName="Afterlife/Prototype Settings")]
    public sealed class PrototypeSettings : ScriptableObject
    {
        public PrototypeCameraSettings Camera;
        public PrototypePresentationSettings Presentation;
        public PrototypeAudioSettings Audio;
        public PrototypeWorldSettings World;
        public PrototypeSystemSettings System;
        public PrototypeUnitData Units;
        public PrototypeWaveData Waves;
        public PrototypeResourceData Resources;
        public PrototypeEconomyData Economy;
        public PrototypeCombatData Combat;
        public PrototypeGrowthData Growth;
        public PrototypeRebirthData Rebirth;
        public PrototypeRelicEffectsData Relics;
        public PrototypeLootData Loot;
        public PrototypeChestData Chests;
        public PrototypeEnvironmentSettings Environment;
        public PrototypeWorldCatalog WorldVisuals;
        public Afterlife.Modules.Destruction.DestructionProfile Destruction;
        private readonly PrototypeEnvironmentValues _defaultEnvironment=new PrototypeEnvironmentValues();
        public PrototypeEnvironmentValues EnvironmentValues=>Environment!=null?Environment.Values:_defaultEnvironment;
        [SerializeField, HideInInspector] private int _dataVersion;
        public int DataVersion => _dataVersion;
        public const int CurrentDataVersion=3;
        public void MarkMigrated() => _dataVersion=CurrentDataVersion;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraGroundDistance")] private float _legacyCameraGroundDistance = 22.825424f;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraEdgeBand")] private float _legacyCameraEdgeBand = 160;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraFastBand")] private float _legacyCameraFastBand = 35;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraMediumBand")] private float _legacyCameraMediumBand = 85;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraSlowSpeed")] private float _legacyCameraSlowSpeed = 4;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraMediumSpeed")] private float _legacyCameraMediumSpeed = 10;
        [SerializeField, HideInInspector, FormerlySerializedAs("CameraMaxSpeed")] private float _legacyCameraMaxSpeed = 20;
        [SerializeField, HideInInspector, FormerlySerializedAs("VisionBlur")] private bool _legacyVisionBlur = true;
        [SerializeField, HideInInspector, FormerlySerializedAs("TreeModelScale")] private float _legacyTreeModelScale = 1.28f/1.8f;
        [SerializeField, HideInInspector, FormerlySerializedAs("RockModelScale")] private float _legacyRockModelScale = 1.20f/1.8f;
        [SerializeField, HideInInspector, FormerlySerializedAs("PixelHeight")] private int _legacyPixelHeight = 360;
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassSizeScale")] private float _legacyGrassSizeScale = 1f/1.8f;
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassUsesTerrainColor")] private bool _legacyGrassUsesTerrainColor = true;
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassBaseColor")] private Color _legacyGrassBaseColor = new Color(.30f,.40f,.23f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassBeigeColor")] private Color _legacyGrassBeigeColor = new Color(.65f,.57f,.41f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassDarkColor")] private Color _legacyGrassDarkColor = new Color(.14f,.17f,.15f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassPaleColor")] private Color _legacyGrassPaleColor = new Color(.91f,.93f,.89f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassTealColor")] private Color _legacyGrassTealColor = new Color(.24f,.48f,.42f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassMossColor")] private Color _legacyGrassMossColor = new Color(.36f,.43f,.20f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassPurpleColor")] private Color _legacyGrassPurpleColor = new Color(.47f,.30f,.48f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassFlowerPink")] private Color _legacyGrassFlowerPink = new Color(.82f,.66f,.78f);
        [SerializeField, HideInInspector, FormerlySerializedAs("GrassFlowerYellow")] private Color _legacyGrassFlowerYellow = new Color(.95f,.92f,.78f);
        [SerializeField, HideInInspector, FormerlySerializedAs("SoundVolume")] private float _legacySoundVolume = .7f;
        [SerializeField, HideInInspector, FormerlySerializedAs("ResourceSpawnSeconds")] private float _legacyResourceSpawnSeconds = 12;
        [SerializeField, HideInInspector, FormerlySerializedAs("TreeLimit")] private int _legacyTreeLimit = 780;
        [SerializeField, HideInInspector, FormerlySerializedAs("RockLimit")] private int _legacyRockLimit = 480;
        [SerializeField, HideInInspector, FormerlySerializedAs("MapSeed")] private int _legacyMapSeed = 0;
        [SerializeField, HideInInspector, FormerlySerializedAs("MapRadius")] private int _legacyMapRadius = 45;
        [SerializeField, HideInInspector, FormerlySerializedAs("FirstPreparation")] private float _legacyFirstPreparation = 60;
        [SerializeField, HideInInspector, FormerlySerializedAs("Maintenance")] private float _legacyMaintenance = 45;
        [SerializeField, HideInInspector, FormerlySerializedAs("CursorRadius")] private float _legacyCursorRadius = .65f;
        [SerializeField, HideInInspector, FormerlySerializedAs("HouseRadius")] private float _legacyHouseRadius = 3.8f;
        [SerializeField, HideInInspector, FormerlySerializedAs("WoodPerSecond")] private float _legacyWoodPerSecond = 2;
        [SerializeField, HideInInspector, FormerlySerializedAs("StonePerSecond")] private float _legacyStonePerSecond = 1;
        [SerializeField, HideInInspector, FormerlySerializedAs("CursorDamage")] private float _legacyCursorDamage = 8;
        [SerializeField, HideInInspector, FormerlySerializedAs("RepairPerSecond")] private float _legacyRepairPerSecond = 12;
        [SerializeField, HideInInspector, FormerlySerializedAs("WarriorSeconds")] private float _legacyWarriorSeconds = 10;
        [SerializeField, HideInInspector, FormerlySerializedAs("WarriorMoney")] private int _legacyWarriorMoney = 10;
        [SerializeField, HideInInspector, FormerlySerializedAs("DaySeconds")] private float _legacyDaySeconds = 35;
        [SerializeField, HideInInspector, FormerlySerializedAs("DuskSeconds")] private float _legacyDuskSeconds = 5;
        [SerializeField, HideInInspector, FormerlySerializedAs("NightSeconds")] private float _legacyNightSeconds = 35;
        [SerializeField, HideInInspector, FormerlySerializedAs("ContributionSeconds")] private float _legacyContributionSeconds = 1;
        [SerializeField, HideInInspector, FormerlySerializedAs("ContributionPower")] private float _legacyContributionPower = 1;
        [SerializeField, HideInInspector, FormerlySerializedAs("DivinePowerMaximum")] private float _legacyDivinePowerMaximum = 100;
        [SerializeField, HideInInspector, FormerlySerializedAs("DivineFieldCost")] private float _legacyDivineFieldCost = 30;
        [SerializeField, HideInInspector, FormerlySerializedAs("DivinePowerRegeneration")] private float _legacyDivinePowerRegeneration = 3;
        [SerializeField, HideInInspector, FormerlySerializedAs("DivineFieldDuration")] private float _legacyDivineFieldDuration = 8;
        [SerializeField, HideInInspector, FormerlySerializedAs("CursorVision")] private float _legacyCursorVision = 4;
        [SerializeField, HideInInspector, FormerlySerializedAs("VillageVision")] private float _legacyVillageVision = 9;
        [SerializeField, HideInInspector, FormerlySerializedAs("VisionTrailSeconds")] private float _legacyVisionTrailSeconds = 1.4f;
        [SerializeField, HideInInspector, FormerlySerializedAs("AutoConstruction")] private float _legacyAutoConstruction = .25f;
        [SerializeField, HideInInspector, FormerlySerializedAs("PrototypeDays")] private int _legacyPrototypeDays = 6;
        public float CameraGroundDistance { get => Camera != null ? Camera.CameraGroundDistance : _legacyCameraGroundDistance; set { if (Camera != null) Camera.CameraGroundDistance=value; else _legacyCameraGroundDistance=value; } }
        public float CameraEdgeBand { get => Camera != null ? Camera.CameraEdgeBand : _legacyCameraEdgeBand; set { if (Camera != null) Camera.CameraEdgeBand=value; else _legacyCameraEdgeBand=value; } }
        public float CameraFastBand { get => Camera != null ? Camera.CameraFastBand : _legacyCameraFastBand; set { if (Camera != null) Camera.CameraFastBand=value; else _legacyCameraFastBand=value; } }
        public float CameraMediumBand { get => Camera != null ? Camera.CameraMediumBand : _legacyCameraMediumBand; set { if (Camera != null) Camera.CameraMediumBand=value; else _legacyCameraMediumBand=value; } }
        public float CameraSlowSpeed { get => Camera != null ? Camera.CameraSlowSpeed : _legacyCameraSlowSpeed; set { if (Camera != null) Camera.CameraSlowSpeed=value; else _legacyCameraSlowSpeed=value; } }
        public float CameraMediumSpeed { get => Camera != null ? Camera.CameraMediumSpeed : _legacyCameraMediumSpeed; set { if (Camera != null) Camera.CameraMediumSpeed=value; else _legacyCameraMediumSpeed=value; } }
        public float CameraMaxSpeed { get => Camera != null ? Camera.CameraMaxSpeed : _legacyCameraMaxSpeed; set { if (Camera != null) Camera.CameraMaxSpeed=value; else _legacyCameraMaxSpeed=value; } }
        public bool VisionBlur { get => Presentation != null ? Presentation.VisionBlur : _legacyVisionBlur; set { if (Presentation != null) Presentation.VisionBlur=value; else _legacyVisionBlur=value; } }
        public float TreeModelScale { get => Presentation != null ? Presentation.TreeModelScale : _legacyTreeModelScale; set { if (Presentation != null) Presentation.TreeModelScale=value; else _legacyTreeModelScale=value; } }
        public float RockModelScale { get => Presentation != null ? Presentation.RockModelScale : _legacyRockModelScale; set { if (Presentation != null) Presentation.RockModelScale=value; else _legacyRockModelScale=value; } }
        public int PixelHeight { get => Presentation != null ? Presentation.PixelHeight : _legacyPixelHeight; set { if (Presentation != null) Presentation.PixelHeight=value; else _legacyPixelHeight=value; } }
        public float GrassSizeScale { get => Presentation != null ? Presentation.GrassSizeScale : _legacyGrassSizeScale; set { if (Presentation != null) Presentation.GrassSizeScale=value; else _legacyGrassSizeScale=value; } }
        public bool GrassUsesTerrainColor { get => Presentation != null ? Presentation.GrassUsesTerrainColor : _legacyGrassUsesTerrainColor; set { if (Presentation != null) Presentation.GrassUsesTerrainColor=value; else _legacyGrassUsesTerrainColor=value; } }
        public Color GrassBaseColor { get => Presentation != null ? Presentation.GrassBaseColor : _legacyGrassBaseColor; set { if (Presentation != null) Presentation.GrassBaseColor=value; else _legacyGrassBaseColor=value; } }
        public Color GrassBeigeColor { get => Presentation != null ? Presentation.GrassBeigeColor : _legacyGrassBeigeColor; set { if (Presentation != null) Presentation.GrassBeigeColor=value; else _legacyGrassBeigeColor=value; } }
        public Color GrassDarkColor { get => Presentation != null ? Presentation.GrassDarkColor : _legacyGrassDarkColor; set { if (Presentation != null) Presentation.GrassDarkColor=value; else _legacyGrassDarkColor=value; } }
        public Color GrassPaleColor { get => Presentation != null ? Presentation.GrassPaleColor : _legacyGrassPaleColor; set { if (Presentation != null) Presentation.GrassPaleColor=value; else _legacyGrassPaleColor=value; } }
        public Color GrassTealColor { get => Presentation != null ? Presentation.GrassTealColor : _legacyGrassTealColor; set { if (Presentation != null) Presentation.GrassTealColor=value; else _legacyGrassTealColor=value; } }
        public Color GrassMossColor { get => Presentation != null ? Presentation.GrassMossColor : _legacyGrassMossColor; set { if (Presentation != null) Presentation.GrassMossColor=value; else _legacyGrassMossColor=value; } }
        public Color GrassPurpleColor { get => Presentation != null ? Presentation.GrassPurpleColor : _legacyGrassPurpleColor; set { if (Presentation != null) Presentation.GrassPurpleColor=value; else _legacyGrassPurpleColor=value; } }
        public Color GrassFlowerPink { get => Presentation != null ? Presentation.GrassFlowerPink : _legacyGrassFlowerPink; set { if (Presentation != null) Presentation.GrassFlowerPink=value; else _legacyGrassFlowerPink=value; } }
        public Color GrassFlowerYellow { get => Presentation != null ? Presentation.GrassFlowerYellow : _legacyGrassFlowerYellow; set { if (Presentation != null) Presentation.GrassFlowerYellow=value; else _legacyGrassFlowerYellow=value; } }
        public float SoundVolume { get => Audio != null ? Audio.SoundVolume : _legacySoundVolume; set { if (Audio != null) Audio.SoundVolume=value; else _legacySoundVolume=value; } }
        public float ResourceSpawnSeconds { get => World != null ? World.ResourceSpawnSeconds : _legacyResourceSpawnSeconds; set { if (World != null) World.ResourceSpawnSeconds=value; else _legacyResourceSpawnSeconds=value; } }
        public int TreeLimit { get => World != null ? World.TreeLimit : _legacyTreeLimit; set { if (World != null) World.TreeLimit=value; else _legacyTreeLimit=value; } }
        public int RockLimit { get => World != null ? World.RockLimit : _legacyRockLimit; set { if (World != null) World.RockLimit=value; else _legacyRockLimit=value; } }
        public int MapSeed { get => World != null ? World.MapSeed : _legacyMapSeed; set { if (World != null) World.MapSeed=value; else _legacyMapSeed=value; } }
        public int MapRadius { get => World != null ? World.MapRadius : _legacyMapRadius; set { if (World != null) World.MapRadius=value; else _legacyMapRadius=value; } }
        public int FertileSiteCount => World != null ? World.FertileSiteCount : 2;
        public int LeylineSiteCount => World != null ? World.LeylineSiteCount : 2;
        public int FertileSiteSize => World != null ? World.FertileSiteSize : 3;
        public int LeylineSiteSize => World != null ? World.LeylineSiteSize : 3;
        public int EconomicSiteMinimumDistance => World != null ? World.EconomicSiteMinimumDistance : 8;
        public float FirstPreparation { get => System != null ? System.FirstPreparation : _legacyFirstPreparation; set { if (System != null) System.FirstPreparation=value; else _legacyFirstPreparation=value; } }
        public float Maintenance { get => System != null ? System.Maintenance : _legacyMaintenance; set { if (System != null) System.Maintenance=value; else _legacyMaintenance=value; } }
        public float CursorRadius { get => System != null ? System.CursorRadius : _legacyCursorRadius; set { if (System != null) System.CursorRadius=value; else _legacyCursorRadius=value; } }
        public float HouseRadius { get => System != null ? System.HouseRadius : _legacyHouseRadius; set { if (System != null) System.HouseRadius=value; else _legacyHouseRadius=value; } }
        public float WoodPerSecond { get => System != null ? System.WoodPerSecond : _legacyWoodPerSecond; set { if (System != null) System.WoodPerSecond=value; else _legacyWoodPerSecond=value; } }
        public float StonePerSecond { get => System != null ? System.StonePerSecond : _legacyStonePerSecond; set { if (System != null) System.StonePerSecond=value; else _legacyStonePerSecond=value; } }
        public float CursorDamage { get => System != null ? System.CursorDamage : _legacyCursorDamage; set { if (System != null) System.CursorDamage=value; else _legacyCursorDamage=value; } }
        public float RepairPerSecond { get => System != null ? System.RepairPerSecond : _legacyRepairPerSecond; set { if (System != null) System.RepairPerSecond=value; else _legacyRepairPerSecond=value; } }
        public float WarriorSeconds { get => System != null ? System.WarriorSeconds : _legacyWarriorSeconds; set { if (System != null) System.WarriorSeconds=value; else _legacyWarriorSeconds=value; } }
        public int WarriorMoney { get => Units!=null?Units.Values.Find(Kind.Warrior).Price:System != null ? System.WarriorMoney : _legacyWarriorMoney; set { if(Units!=null)Units.Values.Find(Kind.Warrior).Price=value;else if (System != null) System.WarriorMoney=value; else _legacyWarriorMoney=value; } }
        public float DaySeconds { get => System != null ? System.DaySeconds : _legacyDaySeconds; set { if (System != null) System.DaySeconds=value; else _legacyDaySeconds=value; } }
        public float DuskSeconds { get => System != null ? System.DuskSeconds : _legacyDuskSeconds; set { if (System != null) System.DuskSeconds=value; else _legacyDuskSeconds=value; } }
        public float NightSeconds { get => System != null ? System.NightSeconds : _legacyNightSeconds; set { if (System != null) System.NightSeconds=value; else _legacyNightSeconds=value; } }
        public float ContributionSeconds { get => System != null ? System.ContributionSeconds : _legacyContributionSeconds; set { if (System != null) System.ContributionSeconds=value; else _legacyContributionSeconds=value; } }
        public float ContributionPower { get => System != null ? System.ContributionPower : _legacyContributionPower; set { if (System != null) System.ContributionPower=value; else _legacyContributionPower=value; } }
        public float DivinePowerMaximum { get => System != null ? System.DivinePowerMaximum : _legacyDivinePowerMaximum; set { if (System != null) System.DivinePowerMaximum=value; else _legacyDivinePowerMaximum=value; } }
        public float DivineFieldCost { get => System != null ? System.DivineFieldCost : _legacyDivineFieldCost; set { if (System != null) System.DivineFieldCost=value; else _legacyDivineFieldCost=value; } }
        public float DivinePowerRegeneration { get => System != null ? System.DivinePowerRegeneration : _legacyDivinePowerRegeneration; set { if (System != null) System.DivinePowerRegeneration=value; else _legacyDivinePowerRegeneration=value; } }
        public float DivineFieldDuration { get => System != null ? System.DivineFieldDuration : _legacyDivineFieldDuration; set { if (System != null) System.DivineFieldDuration=value; else _legacyDivineFieldDuration=value; } }
        public float CursorVision { get => System != null ? System.CursorVision : _legacyCursorVision; set { if (System != null) System.CursorVision=value; else _legacyCursorVision=value; } }
        public float VillageVision { get => System != null ? System.VillageVision : _legacyVillageVision; set { if (System != null) System.VillageVision=value; else _legacyVillageVision=value; } }
        public float VisionTrailSeconds { get => System != null ? System.VisionTrailSeconds : _legacyVisionTrailSeconds; set { if (System != null) System.VisionTrailSeconds=value; else _legacyVisionTrailSeconds=value; } }
        public float AutoConstruction { get => System != null ? System.AutoConstruction : _legacyAutoConstruction; set { if (System != null) System.AutoConstruction=value; else _legacyAutoConstruction=value; } }
        public int PrototypeDays { get => System != null ? System.PrototypeDays : _legacyPrototypeDays; set { if (System != null) System.PrototypeDays=value; else _legacyPrototypeDays=value; } }

        private readonly List<ScriptableObject> _owned = new List<ScriptableObject>();
        public PrototypeSettings RuntimeCopy()
        {
            ValidateData();
            var result=Instantiate(this);
            if(Environment!=null){result.Environment=Instantiate(Environment);result._owned.Add(result.Environment);}
            if(WorldVisuals!=null){result.WorldVisuals=Instantiate(WorldVisuals);result._owned.Add(result.WorldVisuals);}
            if(Destruction!=null){result.Destruction=Instantiate(Destruction);result._owned.Add(result.Destruction);}
            if (Camera != null) { result.Camera=Instantiate(Camera); result._owned.Add(result.Camera); }
            if (Presentation != null) { result.Presentation=Instantiate(Presentation); result._owned.Add(result.Presentation); }
            if (Audio != null) { result.Audio=Instantiate(Audio); result._owned.Add(result.Audio); }
            if (World != null) { result.World=Instantiate(World); result._owned.Add(result.World); }
            if (System != null) { result.System=Instantiate(System); result._owned.Add(result.System); }
            if (Units != null) { result.Units=Instantiate(Units); result._owned.Add(result.Units); }
            if (Waves != null) { result.Waves=Instantiate(Waves); result._owned.Add(result.Waves); }
            if (Resources != null) { result.Resources=Instantiate(Resources); result._owned.Add(result.Resources); }
            if (Economy != null) { result.Economy=Instantiate(Economy); result._owned.Add(result.Economy); }
            if (Combat != null) { result.Combat=Instantiate(Combat); result._owned.Add(result.Combat); }
            if (Growth != null) { result.Growth=Instantiate(Growth); result._owned.Add(result.Growth); }
            if (Rebirth != null) { result.Rebirth=Instantiate(Rebirth); result._owned.Add(result.Rebirth); }
            if (Relics != null) { result.Relics=Instantiate(Relics); result._owned.Add(result.Relics); }
            if (Loot != null) { result.Loot=Instantiate(Loot); result._owned.Add(result.Loot); }
            if (Chests != null) { result.Chests=Instantiate(Chests); result._owned.Add(result.Chests); }
            return result;
        }
        private void OnDestroy() { foreach(var item in _owned) if(item!=null) Destroy(item); }
        public void ValidateData()
        {
            if (System != null && System.MvpSlots)
            {
                if (System.InitialSlots < 1 || System.MaximumSlots < System.InitialSlots || System.MaximumSlots > 3)
                    throw new global::System.InvalidOperationException(name + ": 슬롯 수는 1~3개여야 합니다.");
                foreach (float seconds in new[] { System.SlotRechargeSeconds, System.FirstDaySeconds, System.SecondDaySeconds })
                    if (!(seconds > 0) || float.IsInfinity(seconds))
                        throw new global::System.InvalidOperationException(name + ": MVP 충전·낮 시간은 유한한 양수여야 합니다.");
            }
            if(_dataVersion>=2 && (System==null||World==null||Camera==null||Presentation==null||Audio==null||Units==null||Waves==null||Resources==null||Economy==null||Combat==null||Growth==null||Rebirth==null||Relics==null||Loot==null||Chests==null||Environment==null||WorldVisuals==null||Destruction==null))
                throw new global::System.InvalidOperationException(name+": 분리된 데이터 참조가 누락되었습니다.");
            if(MapRadius<8||MapRadius>120||PrototypeDays<1||DaySeconds<=0||DuskSeconds<=0||NightSeconds<=0||WarriorSeconds<=0||ContributionSeconds<=0||ContributionPower<=0||ResourceSpawnSeconds<=0)
                throw new global::System.InvalidOperationException(name+": 지도 크기(8~120), 시간 및 기여 설정을 확인하세요.");
            if (FertileSiteCount < 0 || LeylineSiteCount < 0 || FertileSiteSize < 1 || LeylineSiteSize < 1 ||
                EconomicSiteMinimumDistance < 0 || FertileSiteSize > MapRadius * 2 + 1 || LeylineSiteSize > MapRadius * 2 + 1)
                throw new global::System.InvalidOperationException(name+": 경제 지대 개수·크기·최소 거리를 확인하세요.");
            if(Environment!=null&&(Environment.Values.TerrainPalette==null||Environment.Values.TerrainPalette.Length!=7||Environment.Values.GrassSamplesPerAxis<1||Environment.Values.LeafCards<1))
                throw new global::System.InvalidOperationException(name+": 지형 팔레트(7개)와 식생 개수를 확인하세요.");
            if(WorldVisuals!=null)
            {
                var ids=new HashSet<string>();
                foreach(var entry in WorldVisuals.Entries)if(entry==null||string.IsNullOrWhiteSpace(entry.Id)||!ids.Add(entry.Id)||entry.Prefab==null)throw new global::System.InvalidOperationException(name+": 월드 리소스 ID/프리팹 오류");
            }
            if(Audio!=null)
            {
                var ids=new HashSet<SoundCue>();
                foreach(var cue in Audio.Cues)if(cue==null||!ids.Add(cue.Id)||cue.Volume<0||cue.Volume>1||cue.Cooldown<0)throw new global::System.InvalidOperationException(name+": 효과음 ID/볼륨/간격 오류");
            }
        }
        public PrototypeRules RulesSnapshot()
        {
            var result=new PrototypeRules
            {
                Units=Units != null ? Units.Snapshot() : new UnitRules(),
                Waves=Waves != null ? Waves.Snapshot() : new WaveRules(),
                Resources=Resources != null ? Resources.Snapshot() : new ResourceRules(),
                Economy=Economy != null ? Economy.Snapshot() : new EconomyRules(),
                Combat=Combat != null ? Combat.Snapshot() : new CombatRules(),
                Growth=Growth != null ? Growth.Snapshot() : new GrowthRules(),
                Rebirth=Rebirth != null ? Rebirth.Snapshot() : new RebirthRules(),
                Relics=Relics != null ? Relics.Snapshot() : new RelicRules(),
                Loot=Loot != null ? Loot.Snapshot() : new LootRules(),
                Chests=Chests != null ? Chests.Snapshot() : new ChestRules(),
            };
            result.Validate();
            return result;
        }
        public Balance ToBalance() => new Balance
        {
            MvpSlots = System != null && System.MvpSlots,
            InitialSlots = System != null ? System.InitialSlots : 1,
            MaximumSlots = System != null ? System.MaximumSlots : 3,
            SlotRechargeSeconds = System != null ? System.SlotRechargeSeconds : 10,
            FirstDaySeconds = System != null ? System.FirstDaySeconds : 90,
            SecondDaySeconds = System != null ? System.SecondDaySeconds : 60,
            Rules = RulesSnapshot(),
            FirstPreparation = FirstPreparation, Maintenance = Maintenance,
            CursorRadius = CursorRadius, HouseRadius = HouseRadius,
            WoodRate = WoodPerSecond, StoneRate = StonePerSecond,
            CursorDamage = CursorDamage, RepairRate = RepairPerSecond,
            WarriorSeconds = WarriorSeconds, WarriorMoney = WarriorMoney,
            DaySeconds = DaySeconds, DuskSeconds = DuskSeconds, NightSeconds = NightSeconds,
            PulseSeconds = ContributionSeconds, ContributionPower = ContributionPower, CursorVision = CursorVision, VillageVision = VillageVision,
            DivinePowerMaximum = DivinePowerMaximum, DivineFieldCost = DivineFieldCost,
            DivinePowerRegeneration = DivinePowerRegeneration, DivineFieldDuration = DivineFieldDuration,
            VisionTrailSeconds = VisionTrailSeconds, AutoConstruction = AutoConstruction, MapRadius = MapRadius, MapSeed=MapSeed, Days = PrototypeDays,ResourceSpawnSeconds=ResourceSpawnSeconds,TreeLimit=TreeLimit,RockLimit=RockLimit,
            FertileSiteCount=FertileSiteCount, LeylineSiteCount=LeylineSiteCount, FertileSiteSize=FertileSiteSize, LeylineSiteSize=LeylineSiteSize,
            EconomicSiteMinimumDistance=EconomicSiteMinimumDistance
        };
    }
}
