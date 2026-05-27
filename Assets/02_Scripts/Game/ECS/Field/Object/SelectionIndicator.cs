using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class SelectionIndicator : Moonstone.Ore.Local.Entity
    {
        private Transform _selectionIndicator;

        protected override void OnInitialize()
        {
            _selectionIndicator = transform.Find("Indicator").Find("Selection");
        }

        public void ShowIndicator()
            => _selectionIndicator.gameObject.SetActive(true);

        public void HideIndicator()
            => _selectionIndicator.gameObject.SetActive(false);
    }
}