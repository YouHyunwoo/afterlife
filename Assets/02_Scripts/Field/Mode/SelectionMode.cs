using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class SelectionMode : Mode.Mode
    {
        [SerializeField] private RaycastSystem _raycastSystem;

        private bool _isCitizenSelected;
        private readonly List<ObjectVisible> _objectVisibles = new();

        private void Update()
        {
            // TODO: 마우스 왼쪽 드래그/클릭 시 유닛 선택
            // TODO: 마우스 오른쪽 드래그/클릭 시 유닛 명령
            if (IsSelectionButtonDown())
                UpdateSelection();
            if (IsCommandButtonDown())
                UpdateCommand();
        }

        private bool IsSelectionButtonDown()
            => Input.GetMouseButtonDown(0);

        private void UpdateSelection()
        {
            if (_raycastSystem.CastToObject(out var selectedObjectVisible))
            {
                Debug.Log("오브젝트 선택: " + selectedObjectVisible);
                foreach (var objectVisible in _objectVisibles)
                    objectVisible.HideSelectionIndicator();
                _objectVisibles.Clear();

                _objectVisibles.Add(selectedObjectVisible);
                selectedObjectVisible.ShowSelectionIndicator();
                _isCitizenSelected = selectedObjectVisible is CitizenVisible;
            }
            else
            {
                Debug.Log("선택 취소");
                foreach (var objectVisible in _objectVisibles)
                    objectVisible.HideSelectionIndicator();
                _objectVisibles.Clear();
            }
        }

        private bool IsCommandButtonDown()
            => Input.GetMouseButtonDown(1);

        private void UpdateCommand()
        {
            if (!_isCitizenSelected) return;
            if (_raycastSystem.CastToObject(out var selectedObjectVisible))
            {
                if (selectedObjectVisible is ResourceVisible resourceVisible)
                {
                    foreach (var objectVisible in _objectVisibles)
                    {
                        if (objectVisible is not CitizenVisible citizenVisible) continue;
                        citizenVisible.DoCommand(CommandType.Harvest, new object[] { resourceVisible });
                    }
                }
                else if (selectedObjectVisible is BuildingVisible buildingVisible)
                {
                    foreach (var objectVisible in _objectVisibles)
                    {
                        if (objectVisible is not CitizenVisible citizenVisible) continue;
                        citizenVisible.DoCommand(CommandType.Build, new object[] { buildingVisible });
                    }
                }
            }
            else if (_raycastSystem.CastToPlane(out var hitPoint))
            {
                var @params = new object[] { hitPoint };
                foreach (var objectVisible in _objectVisibles)
                {
                    if (objectVisible is not CitizenVisible citizenVisible) continue;
                    citizenVisible.DoCommand(CommandType.Move, @params);
                }
            }
        }

        protected override void OnEnter<TParam>(TParam param = null)
        {
            Debug.Log("선택 모드");
        }

        protected override void OnExit<TParam>(TParam param = null)
        {
        }

        public void AddSelectedObjects(ObjectVisible[] objectVisibles)
        {
            _isCitizenSelected = objectVisibles.Length != 1 || objectVisibles[0] is CitizenVisible;
            if (_isCitizenSelected)
            {
                foreach (var objectVisible in objectVisibles)
                {
                    if (objectVisible is CitizenVisible)
                    {
                        _objectVisibles.Add(objectVisible);
                    }
                }
            }
            else
            {
                _objectVisibles.Add(objectVisibles[0]);
            }
        }
    }
}