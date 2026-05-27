// * ComponentManager.Resource

using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public ResourceComponent[] Resources = new ResourceComponent[MAX_ENTITIES];

        public bool[] HasResourceTag = new bool[MAX_ENTITIES];

        public void AddResource(int entityId, int woods, int stones, float harvestSpeed)
        {
            Resources[entityId].Woods = woods;
            Resources[entityId].Stones = stones;
            Resources[entityId].State = HarvestState.None;
            Resources[entityId].HarvestRate = 0f;
            Resources[entityId].HarvestSpeed = harvestSpeed;
            Resources[entityId].WorkerCount = 0;
            HasResourceTag[entityId] = true;
        }

        private void RemoveHarvest(int entityId)
        {
            HasResourceTag[entityId] = false;
        }
    }
}
