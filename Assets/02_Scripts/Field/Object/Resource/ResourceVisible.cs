using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class ResourceVisible : ObjectVisible
    {
        private int _woods;
        private int _stones;
        private HoldableVisible _resourceHoldableVisiblePrefab;
        private float _baseHarvestSpeed = 0f;
        private float _harvestSpeed;

        private float _elapsedTime;
        public bool IsHarvested => _elapsedTime >= 1f;

        public event Action<HoldableVisible, ResourceVisible, object> OnHarvested;

        protected override void OnInitialize()
        {
            base.OnInitialize();

            _harvestSpeed = _baseHarvestSpeed;
        }

        public override void SetData(ObjectData data)
        {
            base.SetData(data);
            var resourceData = data as ResourceData;
            _woods = resourceData.Woods;
            _stones = resourceData.Stones;
            _resourceHoldableVisiblePrefab = resourceData.ResourceHoldableVisiblePrefab;
        }

        public void AttachCitizen(CitizenVisible citizenVisible)
        {
            Debug.Log(citizenVisible.name + " 붙음");
            _harvestSpeed += 0.2f;
        }

        public void DetachCitizen(CitizenVisible citizenVisible)
        {
            Debug.Log(citizenVisible.name + " 떨어짐");
            _harvestSpeed -= 0.2f;
        }

        private void Update()
        {
            _elapsedTime += Time.deltaTime * _harvestSpeed;
            if (_elapsedTime >= 1f)
            {
                FinishHarvest();
                enabled = false;
            }
        }

        public void FinishHarvest()
        {
            var holdableVisible = Instantiate(_resourceHoldableVisiblePrefab);
            holdableVisible.Holdings.Add("Woods", _woods);
            holdableVisible.Holdings.Add("Stones", _stones);
            OnHarvested?.Invoke(holdableVisible, this, this);
        }
    }
}