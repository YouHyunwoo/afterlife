using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class ConstructionMode : Moonstone.Ore.Local.Entity
    {
        [SerializeField] private RaycastSystem _raycastSystem;
        [SerializeField] private ConstructionGuideSystem _constructionGuideSystem;

        private ObjectVisible _currentObjectVisible;
        // private ObjectVisible _previewObjectVisible;

        public event Action<Vector2Int, ObjectVisible, ConstructionMode, object> OnConfirmed;
        public event Action<Vector2Int, ObjectVisible, ConstructionMode, object> OnCanceled;

        private void OnEnable()
        {
            
        }

        private void Update()
        {
            UpdateGuidance();
        }

        private void UpdateGuidance()
        {
            var isHit = _raycastSystem.Cast(out var hitPoint);
            if (!isHit) return;

            var gridSize = _currentObjectVisible.Size;
            var gridPosition = new Vector2Int(Mathf.RoundToInt(hitPoint.x - gridSize.x / 2f), Mathf.RoundToInt(hitPoint.y - gridSize.y / 2f));
            _constructionGuideSystem.ShowGuide(gridPosition, gridSize, true);
            // _previewObjectVisible.transform.position = new Vector3(gridPosition.x + gridSize.x / 2f, gridPosition.y + gridSize.y / 2f, 0);

            if (Input.GetMouseButtonDown(0))
            {
                OnConfirmed?.Invoke(gridPosition, _currentObjectVisible, this, null);
                Exit();
            }
            else if (Input.GetMouseButtonDown(1))
            {
                OnCanceled?.Invoke(gridPosition, _currentObjectVisible, this, null);
                Exit();
            }
        }

        private void OnDisable()
        {
            
        }

        public void Enter(ObjectVisible objectVisible)
        {
            _currentObjectVisible = objectVisible;
            // _previewObjectVisible = Instantiate(objectVisible);
            _constructionGuideSystem.SetUp();
            enabled = true;

            UpdateGuidance();
        }

        public void Exit()
        {
            enabled = false;
            _constructionGuideSystem.TearDown();
            // _previewObjectVisible = null;
            _currentObjectVisible = null;
        }
    }
}