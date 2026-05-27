using System;
using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public class BuildingSystem : ISystem<ComponentManager>
    {
        public Action<int> OnBuilt;

        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasBuildingTag[i]) continue;
                UpdateBuild(i, deltaTime, cm);
            }
        }

        private void UpdateBuild(int entityId, float deltaTime, ComponentManager cm)
        {
            ref var building = ref cm.Buildings[entityId];

            if (building.IsBuilt) return;
            if (building.Mode != BuildingMode.Preview) return;
            // if (building.WorkerCount <= 0) return;

            building.BuildRate += deltaTime * building.BuildSpeed;

            if (building.BuildRate >= 1f)
                FinishBuild(entityId, cm);
        }

        private void FinishBuild(int entityId, ComponentManager cm)
        {
            ref var building = ref cm.Buildings[entityId];
            building.BuildRate = 1f;
            building.BuildSpeed -= building.WorkerCount * 0.2f;
            building.WorkerCount = 0;
            building.State = BuildingState.Built;
            building.Mode = BuildingMode.Normal;

            if (cm.HasBuildingModeSetter[entityId])
                cm.BuildingModeSetters[entityId].SetMode(BuildingMode.Normal);

            OnBuilt?.Invoke(entityId);
        }
    }
}
