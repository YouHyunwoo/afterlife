// * ComponentManager.Citizen

using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public HarvestTargetComponent[] HarvestTargets = new HarvestTargetComponent[MAX_ENTITIES];
        public ReturnTargetComponent[] ReturnTargets = new ReturnTargetComponent[MAX_ENTITIES];
        public BuildTargetComponent[] BuildTargets = new BuildTargetComponent[MAX_ENTITIES];

        public bool[] HasCitizenTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenIdleTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenMoveTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenCommandedMoveTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenChaseTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenFightTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenBuildTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenHarvestTag = new bool[MAX_ENTITIES];
        public bool[] HasCitizenReturnTag = new bool[MAX_ENTITIES];
        public bool[] HasHarvestTarget = new bool[MAX_ENTITIES];
        public bool[] HasReturnTarget = new bool[MAX_ENTITIES];
        public bool[] HasBuildTarget = new bool[MAX_ENTITIES];

        public void AddHarvestTarget(int entityId, int resourceEntityId)
        {
            HarvestTargets[entityId].ResourceEntityId = resourceEntityId;
            HarvestTargets[entityId].IsHarvesting = false;
            HasHarvestTarget[entityId] = true;
        }

        private void RemoveHarvestTarget(int entityId)
        {
            HasHarvestTarget[entityId] = false;
        }

        public void AddReturnTarget(int entityId, int buildingEntityId, Vector3 destination)
        {
            ReturnTargets[entityId].BuildingEntityId = buildingEntityId;
            ReturnTargets[entityId].Destination = destination;
            ReturnTargets[entityId].IsFound = true;
            HasReturnTarget[entityId] = true;
        }

        private void RemoveReturnTarget(int entityId)
        {
            HasReturnTarget[entityId] = false;
        }

        public void AddBuildTarget(int entityId, int buildingEntityId)
        {
            BuildTargets[entityId].BuildingEntityId = buildingEntityId;
            BuildTargets[entityId].IsBuilding = false;
            HasBuildTarget[entityId] = true;
        }

        private void RemoveBuildTarget(int entityId)
        {
            BuildTargets[entityId].BuildingEntityId = -1;
            BuildTargets[entityId].IsBuilding = false;
            HasBuildTarget[entityId] = false;
        }
    }
}
