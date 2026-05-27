using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class ItemHolder : Moonstone.Ore.Local.Entity
    {
        [SerializeField] private Transform _woodHoldingPrefab;
        [SerializeField] private Transform _stoneHoldingPrefab;
        [SerializeField] private Transform _holdingContainerTransform;

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