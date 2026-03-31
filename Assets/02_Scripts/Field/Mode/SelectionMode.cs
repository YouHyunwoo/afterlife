using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class SelectionMode : Mode.Mode
    {
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
            if (FieldCursor.CastToObject(out var selectedObjectVisible))
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
            if (FieldCursor.CastToObject(out var selectedObjectVisible))
            {
                if (selectedObjectVisible is ResourceVisible resourceVisible)
                {
                    foreach (var objectVisible in _objectVisibles)
                    {
                        if (objectVisible is not CitizenVisible citizenVisible) continue;
                        citizenVisible.Object.DoCommand(CommandType.Harvest, new object[] { resourceVisible.Object });
                    }
                }
                else if (selectedObjectVisible is BuildingVisible buildingVisible)
                {
                    foreach (var objectVisible in _objectVisibles)
                    {
                        if (objectVisible is not CitizenVisible citizenVisible) continue;
                        citizenVisible.Object.DoCommand(CommandType.Build, new object[] { buildingVisible.Object });
                    }
                }
                else if (selectedObjectVisible is EnemyVisible enemyVisible)
                {
                    foreach (var objectVisible in _objectVisibles)
                    {
                        if (objectVisible is not CitizenVisible citizenVisible) continue;
                        citizenVisible.Object.DoCommand(CommandType.Fight, new object[] { enemyVisible.Object });
                    }
                }
            }
            else if (FieldCursor.CastToPlane(out var hitPoint))
            {
                var @params = new object[] { hitPoint };
                foreach (var objectVisible in _objectVisibles)
                {
                    if (objectVisible is not CitizenVisible citizenVisible) continue;
                    citizenVisible.Object.DoCommand(CommandType.Move, @params);
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
    }
}