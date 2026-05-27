using ECS.Dev;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class BuildingModeSetter : Moonstone.Ore.Local.Entity
    {
        private SpriteRenderer _bodySpriteRenderer;
        private SpriteRenderer _previewSpriteRenderer;

        private void OnDrawGizmos()
        {
            if (!TryGetComponent(out Entity entity)) return;
            var cm = entity.ComponentManager as ComponentManager;
            if (cm == null) return;
            if (!cm.HasBuildingTag[entity.Id]) return;
            var building = cm.Buildings[entity.Id];
            Handles.Label(transform.position + Vector3.down, $"{building.BuildRate}");
        }

        protected override void OnInitialize()
        {
            transform.Find("Root").Find("Body").TryGetComponent(out _bodySpriteRenderer);
            transform.Find("Root").Find("Preview").TryGetComponent(out _previewSpriteRenderer);
        }

        public void SetMode(BuildingMode mode)
        {
            switch (mode)
            {
                case BuildingMode.Preview:
                    _bodySpriteRenderer.gameObject.SetActive(false);
                    _previewSpriteRenderer.gameObject.SetActive(true);
                    break;
                case BuildingMode.Normal:
                    _bodySpriteRenderer.gameObject.SetActive(true);
                    _previewSpriteRenderer.gameObject.SetActive(false);
                    break;
            }
        }
    }
}
