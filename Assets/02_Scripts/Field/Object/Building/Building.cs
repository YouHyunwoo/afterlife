using System;
using System.Collections.Generic;

namespace Afterlife.Dev.Field
{
    public record struct ModeInfo(BuildingMode BuildingMode);
    public record struct BuildInfo(List<Citizen> Workers, float BuildRate);

    public class Building : Object
    {
        protected BuildingType buildingType;
        protected float townZoneRadius;
        protected float buildSpeed;
        protected float buildRate;
        protected BuildingState state = BuildingState.Building;
        protected BuildingMode mode = BuildingMode.Preview;
        protected readonly List<Citizen> workers = new();

        public BuildingType BuildingType => buildingType;
        public float TownZoneRadius => townZoneRadius;
        public float BuildRate => buildRate;
        public bool IsBuilt => buildRate >= 1f;
        public BuildingState BuildingState => state;

        public event Action<Building, object> OnBuilt;
        public event Action<ModeInfo, Building, object> OnModeChanged;
        public event Action<BuildInfo, Building, object> OnWorkerAdded;
        public event Action<BuildInfo, Building, object> OnWorkerRemoved;

        public Building(string id) : base(id) { }

        public override void Initialize(ObjectData data) => Initialize((BuildingData)data);

        public void Initialize(BuildingData data)
        {
            base.Initialize(data);

            buildingType = data.BuildingType;
            townZoneRadius = data.TownZoneRadius;
            buildSpeed = data.BuildSpeed;
        }

        public override void Update(float deltaTime)
        {
            if (buildRate >= 1f) return;

            var canBuild = (
                mode == BuildingMode.Normal &&
                state == BuildingState.Building
            );

            if (canBuild)
            {
                buildRate += deltaTime * buildSpeed; // TODO: Duration으로 해야하나?
                if (buildRate >= 1f)
                    FinishBuild();
            }
        }

        public void FinishBuild()
        {
            state = BuildingState.Built;
            OnBuilt?.Invoke(this, this);
        }

        public void SetMode(BuildingMode mode)
        {
            this.mode = mode;
            OnModeChanged?.Invoke(new ModeInfo(mode), this, this);
        }

        public void AddWorker(Citizen citizen)
        {
            buildSpeed += 0.2f; // TODO: citizen harvest speed
            workers.Add(citizen);
            OnWorkerAdded?.Invoke(new BuildInfo(workers, buildRate), this, this);
        }

        public void RemoveWorker(Citizen citizen)
        {
            buildSpeed -= 0.2f;
            workers.Remove(citizen);
            OnWorkerRemoved?.Invoke(new BuildInfo(workers, buildRate), this, this);
        }
    }
}