using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenVisible : CharacterVisible<Citizen>
    {
        [SerializeField] private Transform _woodHoldingPrefab;
        [SerializeField] private Transform _stoneHoldingPrefab;
        private Transform _holdingContainerTransform;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
#if UNITY_EDITOR
            var stateName = @object.StateName;
            UnityEditor.Handles.Label(transform.position, $"State: {stateName}");
#endif
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _holdingContainerTransform = transform.Find("Root").Find("Body").Find("Holdings");
        }

        public void TakeHoldings(int woods, int stones)
        {
            if (woods > 0)
                Instantiate(_woodHoldingPrefab, _holdingContainerTransform, false);
            else if (stones > 0)
                Instantiate(_stoneHoldingPrefab, _holdingContainerTransform, false);
        }

        public void DropHoldings()
        {
            foreach (Transform child in _holdingContainerTransform)
                Destroy(child.gameObject);
        }
    }
}