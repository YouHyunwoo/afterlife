using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Building Data")]
    public sealed class PrototypeBuildingData : ScriptableObject
    {
        public Kind Id;
        public string DisplayName;
        public Sprite Icon;
        [Min(0)] public int GoldCost;
        public bool RequiresBuilding;
        public Kind RequiredBuilding=Kind.Altar;
        [Tooltip("Vision Radius와 Territory Radius는 건물 중심 기준 XZ 평면의 Unity 월드 반경입니다. Light Radius는 광원 기준 3D 월드 반경으로 주변 밝기만 조절합니다. 세 반경은 독립적이며 0은 비활성입니다. 영역은 타일 중심이 반경 이내인 타일만 포함하며 집·마을회관에 적용됩니다. 변경은 새 게임부터 적용됩니다.")]
        public BuildingStats Stats;
        [SerializeField,HideInInspector] private bool _statsMigrated;
        [SerializeField,HideInInspector] private int _buildingRulesVersion;
        public int BuildingRulesVersion=>_buildingRulesVersion;
        public void MigrateBuildingRules()
        {
            if(_buildingRulesVersion>=1)return;
            MigrateStats();var defaults=BuildingStats.Defaults(Id);
            Stats.Depth=defaults.Depth;Stats.VisionRadius=defaults.VisionRadius;Stats.TerritoryRadius=defaults.TerritoryRadius;
            Stats.LightRadius=defaults.LightRadius;
            Stats.ConstructionSpeedBonus=defaults.ConstructionSpeedBonus;
            Stats.BlocksMovement=defaults.BlocksMovement;Stats.DirectBuild=defaults.DirectBuild;Stats.CanMerge=defaults.CanMerge;Stats.MergeTarget=defaults.MergeTarget;
            Stats.Production=defaults.Production;Stats.ProducedUnit=defaults.ProducedUnit;Stats.ProductionSeconds=defaults.ProductionSeconds;
            Stats.ProductionAmount=defaults.ProductionAmount;Stats.TroopCapacity=defaults.TroopCapacity;Stats.AccelerationGoldCost=defaults.AccelerationGoldCost;
            Stats.PopulationCapacity=0;_buildingRulesVersion=1;
        }
        public void MigrateStats() { if(_statsMigrated)return; Stats=BuildingStats.Defaults(Id);_statsMigrated=true; }
        public bool StatsMigrated=>_statsMigrated;
        public ConstructionRule ToRule()=>new ConstructionRule{Id=Id,DisplayName=DisplayName,Stats=JsonUtility.FromJson<BuildingStats>(JsonUtility.ToJson(_statsMigrated?Stats:BuildingStats.Defaults(Id))),GoldCost=GoldCost,RequiresBuilding=RequiresBuilding,RequiredBuilding=RequiredBuilding};
    }
}
