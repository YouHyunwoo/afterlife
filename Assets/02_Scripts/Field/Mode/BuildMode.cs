using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildModeParam : Mode.ModeParam
    {
        public ObjectVisible ObjectVisiblePrefab;
    }

    public class BuildMode : Mode.Mode
    {
        [SerializeField] private RaycastSystem _raycastSystem;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private BuildGuideSystem _buildGuideSystem;

        private ObjectVisible _currentObjectVisiblePrefab;
        private ObjectVisible _previewObjectVisible;

        public event Action<Vector2Int, ObjectVisible, BuildMode, object> OnConfirmed;
        public event Action<Vector2Int, ObjectVisible, BuildMode, object> OnCanceled;

        private void Update()
        {
            UpdateGuidance();
        }

        private void UpdateGuidance()
        {
            var isHit = _raycastSystem.Cast(out var hitPoint);
            if (!isHit) return;

            var gridSize = _currentObjectVisiblePrefab.Size;
            var gridPosition = new Vector2Int(Mathf.RoundToInt(hitPoint.x - gridSize.x / 2f), Mathf.RoundToInt(hitPoint.y - gridSize.y / 2f));
            var canBuild = (
                _gridSystem.IsPassable(GridLayer.Terrain, gridPosition, gridSize) &&
                _gridSystem.IsPassable(GridLayer.Field, gridPosition, gridSize)
            );
            _buildGuideSystem.ShowGuide(gridPosition, gridSize, canBuild);
            _previewObjectVisible.transform.position = new Vector3(gridPosition.x + gridSize.x / 2f, gridPosition.y + gridSize.y / 2f, 0);

            if (Input.GetMouseButtonDown(0))
            {
                OnConfirmed?.Invoke(gridPosition, _currentObjectVisiblePrefab, this, this);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                OnCanceled?.Invoke(gridPosition, _currentObjectVisiblePrefab, this, this);
            }
        }

        protected override void OnEnter<TParam>(TParam param = null)
        {
            Debug.Log("건설 모드");
            if (param is BuildModeParam buildModeParam)
            {
                _currentObjectVisiblePrefab = buildModeParam.ObjectVisiblePrefab;
                _previewObjectVisible = Instantiate(_currentObjectVisiblePrefab);
                _previewObjectVisible.name += "(Preview)";
                if (_previewObjectVisible is BuildingVisible buildingVisible)
                    buildingVisible.SetPreviewMode();
                _buildGuideSystem.SetUp();
                UpdateGuidance();
            }
        }

        protected override void OnExit<TParam>(TParam param = null)
        {
            _buildGuideSystem.TearDown();
            if (_previewObjectVisible != null)
                Destroy(_previewObjectVisible.gameObject);
            _previewObjectVisible = null;
            _currentObjectVisiblePrefab = null;
        }
    }
}