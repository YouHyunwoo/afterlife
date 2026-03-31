using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public record struct HarvestResultInfo(Dictionary<string, float> Holdings);
    public record struct HarvestInfo(List<Citizen> Workers, float Rate);

    public class Resource : Object
    {
        protected int woods;
        protected int stones;
        protected float harvestSpeed;
        protected float harvestRate;
        protected readonly List<Citizen> workers = new();

        public int Woods => woods;
        public int Stones => stones;
        public bool IsHarvested => harvestRate >= 1f;

        public event Action<HarvestResultInfo, Resource, object> OnHarvested;
        public event Action<HarvestInfo, Resource, object> OnWorkerAdded;
        public event Action<HarvestInfo, Resource, object> OnWorkerRemoved;

        public Resource(string id) : base(id) { }

        public override void Initialize(ObjectData data) => Initialize((ResourceData)data);

        public void Initialize(ResourceData data)
        {
            base.Initialize(data);

            woods = data.Woods;
            stones = data.Stones;
        }

        public override void Update(float deltaTime)
        {
            if (harvestRate >= 1f) return;

            harvestRate += deltaTime * harvestSpeed;
            if (harvestRate >= 1f)
                FinishHarvest();
        }

        public void FinishHarvest()
        {
            var count = workers.Count;
            Debug.Log("workers count: " + count);
            if (count > 0)
            {
                int woodsBase = woods / count, woodsRem = woods % count;
                int stonesBase = stones / count, stonesRem = stones % count;
                for (int i = 0; i < count; i++)
                {
                    int w = woodsBase + (i < woodsRem ? 1 : 0);
                    int s = stonesBase + (i < stonesRem ? 1 : 0);
                    workers[i].TakeHoldings(w, s);
                }
            }
            var holdings = new Dictionary<string, float>()
            {
                ["Woods"] = woods,
                ["Stones"] = stones,
            };
            OnHarvested?.Invoke(new HarvestResultInfo(holdings), this, this);
        }

        public void AddWorker(Citizen citizen)
        {
            harvestSpeed += 1.2f; // TODO: citizen harvest speed
            workers.Add(citizen);
            OnWorkerAdded?.Invoke(new HarvestInfo(workers, harvestRate), this, this);
        }

        public void RemoveWorker(Citizen citizen)
        {
            OnWorkerRemoved?.Invoke(new HarvestInfo(workers, harvestRate), this, this);
            workers.Remove(citizen);
            harvestSpeed -= 0.2f;
        }
    }
}