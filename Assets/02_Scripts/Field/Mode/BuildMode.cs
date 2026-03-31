using System;
using System.Linq;
using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildModeParam : Mode.ModeParam
    {
        public BuildingVisible Prefab;
        public Vector2Int Size;
    }

    public class BuildMode : Mode.Mode
    {
        [SerializeField] private BuildGuideSystem _buildGuideSystem;

        private WorldRepository _worldRepository;
        private World.World _world;

        private BuildModeParam _param;
        private BuildingVisible _previewBuildingVisible;

        public event Action<Vector2Int, ObjectVisible, BuildMode, object> OnConfirmed;
        public event Action<Vector2Int, ObjectVisible, BuildMode, object> OnCanceled;

        private void Update()
        {
            UpdateGuidance();
        }

        private void UpdateGuidance()
        {
            var isHit = FieldCursor.CastToPlane(out var hitPoint);
            if (!isHit) return;

            var size = _param.Size;
            var position = new Vector2Int(Mathf.RoundToInt(hitPoint.x - size.x / 2f), Mathf.RoundToInt(hitPoint.y - size.y / 2f));
            var canBuild = _world.WorldMap.IsPassable(position, size);
            _previewBuildingVisible.transform.position = new Vector3(position.x + size.x / 2f, position.y + size.y / 2f, 0);
            _buildGuideSystem.ShowGuide(position, size, canBuild);

            if (Input.GetMouseButtonDown(0))
            {
                OnConfirmed?.Invoke(position, _param.Prefab, this, this);
            }
            else if (Input.GetMouseButtonDown(1))
            {
                OnCanceled?.Invoke(position, _param.Prefab, this, this);
            }
        }

        protected override void OnEnter<TParam>(TParam param = null)
        {
            Debug.Log("건설 모드");

            _world = _worldRepository.FindAll().First();
            if (param is BuildModeParam buildModeParam)
            {
                _param = buildModeParam;
                _previewBuildingVisible = Instantiate(_param.Prefab);
                _previewBuildingVisible.name += "(Preview)";
                _previewBuildingVisible.SetMode(BuildingMode.Preview);
                _buildGuideSystem.SetUp();
                UpdateGuidance();
            }
        }

        protected override void OnExit<TParam>(TParam param = null)
        {
            _buildGuideSystem.TearDown();
            if (_previewBuildingVisible != null)
                Destroy(_previewBuildingVisible.gameObject);
            _previewBuildingVisible = null;
            _param = null;
        }
    }
}