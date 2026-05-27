// * ComponentManager.Building

using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public BuildingComponent[] Buildings = new BuildingComponent[MAX_ENTITIES];
        public BuildingModeSetter[] BuildingModeSetters = new BuildingModeSetter[MAX_ENTITIES];

        public bool[] HasBuildingTag = new bool[MAX_ENTITIES];
        public bool[] HasBuildingModeSetter = new bool[MAX_ENTITIES];

        public bool[] HasHouseTag = new bool[MAX_ENTITIES];

        public void AddBuilding(int entityId, BuildingType buildingType, float buildSpeed, float townZoneRadius)
        {
            Buildings[entityId].Type = buildingType;
            Buildings[entityId].Mode = BuildingMode.Preview;
            Buildings[entityId].State = BuildingState.Building;
            Buildings[entityId].BuildRate = 0f;
            Buildings[entityId].BuildSpeed = buildSpeed;
            Buildings[entityId].TownZoneRadius = townZoneRadius;
            Buildings[entityId].WorkerCount = 0;
            HasBuildingTag[entityId] = true;
        }

        public void AddBuildingModeSetter(int entityId, BuildingModeSetter building)
        {
            BuildingModeSetters[entityId] = building;
            HasBuildingModeSetter[entityId] = true;
        }

        private void RemoveBuilding(int entityId)
        {
            HasBuildingTag[entityId] = false;
        }

        private void RemoveBuildingModeSetter(int entityId)
        {
            BuildingModeSetters[entityId] = null;
            HasBuildingModeSetter[entityId] = false;
        }
    }
}
