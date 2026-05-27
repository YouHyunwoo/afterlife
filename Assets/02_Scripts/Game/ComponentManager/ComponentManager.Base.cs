// * ComponentManager.Base

using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public const int MAX_ENTITIES = 5000;

        public bool[] HasDespawnTag = new bool[MAX_ENTITIES];

        public void HandleEntityDestroyed(int entityId)
        {
            // Object
            RemoveEntity(entityId);
            RemoveSelectCommand(entityId);
            HasSelectedTag[entityId] = false;

            // Placeable
            RemoveVolume(entityId);

            // Agent
            RemoveMoveCommand(entityId);
            RemoveNavMeshAgent(entityId);
            RemoveWander(entityId);
            RemoveAttack(entityId);
            RemoveChaseTarget(entityId);
            RemoveHolder(entityId);

            // Citizen
            HasCitizenTag[entityId] = false;
            HasCitizenIdleTag[entityId] = false;
            HasCitizenMoveTag[entityId] = false;
            HasCitizenChaseTag[entityId] = false;
            HasCitizenFightTag[entityId] = false;
            HasCitizenBuildTag[entityId] = false;
            HasCitizenHarvestTag[entityId] = false;
            HasCitizenReturnTag[entityId] = false;
            RemoveHarvestTarget(entityId);
            RemoveReturnTarget(entityId);
            RemoveBuildTarget(entityId);

            // Enemy
            HasEnemyTag[entityId] = false;
            HasEnemyMoveTag[entityId] = false;
            HasEnemyFightTag[entityId] = false;

            // Object type tags
            HasHouseTag[entityId] = false;
            HasBossTag[entityId] = false;

            // Building
            RemoveBuilding(entityId);
            RemoveBuildingModeSetter(entityId);

            // Fog
            RemoveLight(entityId);
            RemoveFogAlpha(entityId);
        }
    }
}