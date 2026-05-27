using System;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class BuildModeParam : Mode.ModeParam
    {
        public PlaceableData Data;
    }

    public class BuildMode : Mode.Mode
    {
        private BuildGuideSystem _buildGuideSystem;

        private Game.World _world;

        public void SetBuildGuideSystem(BuildGuideSystem buildGuideSystem)
            => _buildGuideSystem = buildGuideSystem;

        private BuildModeParam _param;
        private GameObject _preview;

        public event Action<Vector2Int, BuildModeParam, BuildMode, object> OnConfirmed;
        public event Action<Vector2Int, BuildModeParam, BuildMode, object> OnCanceled;

        private void Update()
        {
            UpdateGuidance();
        }

        private void UpdateGuidance()
        {
            if (_param == null) return;
            var isHit = FieldCursor.CastToPlane(out var hitPoint);
            if (!isHit) return;

            var size = _param.Data.Volume2DSize;
            var position = new Vector2Int(Mathf.RoundToInt(hitPoint.x - size.x / 2f), Mathf.RoundToInt(hitPoint.y - size.y / 2f));
            var canBuild = _world.IsPassable(position, size);
            _preview.transform.position = new Vector3(position.x + size.x / 2f, position.y + size.y / 2f, 0);
            _buildGuideSystem.ShowGuide(position, size, canBuild);

            if (Input.GetMouseButtonDown(0))
                OnConfirmed?.Invoke(position, _param, this, this);
            else if (Input.GetMouseButtonDown(1))
                OnCanceled?.Invoke(position, _param, this, this);
        }

        protected override void OnEnter<TParam>(TParam param = null)
        {
            Debug.Log("건설 모드");

            if (param is BuildModeParam buildModeParam)
            {
                _param = buildModeParam;
                _preview = Instantiate(_param.Data.Prefab);
                _preview.name += "(Preview)";
                _preview.TryGetComponent(out BuildingModeSetter building);
                building.SetMode(BuildingMode.Preview);
                _buildGuideSystem.SetUp();
                UpdateGuidance();
            }
        }

        protected override void OnExit<TParam>(TParam param = null)
        {
            _buildGuideSystem.TearDown();
            if (_preview != null)
                Destroy(_preview.gameObject);
            _preview = null;
            _param = null;
        }
    }
}