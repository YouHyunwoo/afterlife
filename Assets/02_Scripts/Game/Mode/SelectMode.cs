using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class SelectMode : Mode.Mode
    {
        private ComponentManager _cm;

        private readonly List<int> _entityIds = new();
        private bool _isCitizenSelected;

        private void Update()
        {
            if (IsSelectionButtonDown())
                UpdateSelection();
            if (IsCommandButtonDown())
                UpdateCommand();
        }

        private bool IsSelectionButtonDown()
            => Input.GetMouseButtonDown(0);

        private void UpdateSelection()
        {
            if (FieldCursor.CastToEntity(out var entity))
            {
                Debug.Log("오브젝트 선택: " + entity);

                foreach (var entityId in _entityIds)
                {
                    if (entity.Id == entityId) continue;
                    if (_cm.HasSelectCommand[entityId] &&
                        _cm.HasSelectedTag[entityId])
                        _cm.SelectCommands[entityId].Type = SelectType.Deselect;
                }
                _entityIds.Clear();

                _entityIds.Add(entity.Id);
                if (_cm.HasSelectCommand[entity.Id] &&
                    !_cm.HasSelectedTag[entity.Id])
                {
                    _cm.SelectCommands[entity.Id].Type = SelectType.Select;
                }

                _isCitizenSelected = _cm.HasCitizenTag[entity.Id];
            }
            else
            {
                Debug.Log("선택 취소");
                foreach (var entityId in _entityIds)
                    if (_cm.HasSelectCommand[entityId] &&
                        _cm.HasSelectedTag[entityId])
                        _cm.SelectCommands[entityId].Type = SelectType.Deselect;
                _entityIds.Clear();
                _isCitizenSelected = false;
            }
        }

        private bool IsCommandButtonDown()
            => Input.GetMouseButtonDown(1);

        private void UpdateCommand()
        {
            if (!_isCitizenSelected) return;

            if (FieldCursor.CastToEntity(out var entity)) // * 클릭한 Entity가...
            {
                if (_cm.HasResourceTag[entity.Id]) // * 자원이면
                {
                    foreach (var entityId in _entityIds)
                    {
                        if (!_cm.HasCitizenTag[entityId]) continue;
                        ClearCitizenStateTags(entityId);
                        _cm.HasCitizenHarvestTag[entityId] = true;
                        _cm.AddHarvestTarget(entityId, entity.Id);
                        _cm.MoveCommands[entityId].Destination = entity.transform.position;
                        _cm.MoveCommands[entityId].HasNewTarget = true;
                    }
                }
                else if (_cm.HasBuildingTag[entity.Id]) // * 건물이면
                {
                    ref var building = ref _cm.Buildings[entity.Id];

                    if (building.Type == BuildingType.House && building.IsBuilt)
                    {
                        foreach (var entityId in _entityIds)
                        {
                            if (!_cm.HasCitizenTag[entityId]) continue;
                            ClearCitizenStateTags(entityId);

                            if (_cm.HasHolder[entityId])
                            {
                                // 소지품이 있으면 귀환
                                _cm.HasCitizenReturnTag[entityId] = true;
                            }
                            else
                            {
                                // 집 근처 임의 위치로 이동
                                var angle = Random.Range(0f, Mathf.PI * 2f);
                                var radius = Random.Range(0f, _cm.Buildings[entity.Id].TownZoneRadius);
                                var randomPosition = entity.transform.position + new Vector3(
                                    Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                                _cm.HasCitizenMoveTag[entityId] = true;
                                _cm.HasCitizenCommandedMoveTag[entityId] = true;
                                _cm.MoveCommands[entityId].Destination = randomPosition;
                                _cm.MoveCommands[entityId].HasNewTarget = true;
                            }
                        }
                    }
                    else
                    {
                        foreach (var entityId in _entityIds)
                        {
                            if (!_cm.HasCitizenTag[entityId]) continue;
                            ClearCitizenStateTags(entityId);
                            _cm.HasCitizenBuildTag[entityId] = true;
                            _cm.AddBuildTarget(entityId, entity.Id);
                            _cm.MoveCommands[entityId].Destination = entity.transform.position;
                            _cm.MoveCommands[entityId].HasNewTarget = true;
                        }
                    }
                }
                else if (_cm.HasEnemyTag[entity.Id]) // * 적이면
                {
                    foreach (var entityId in _entityIds)
                    {
                        if (!_cm.HasCitizenTag[entityId]) continue;
                        ClearCitizenStateTags(entityId);
                        _cm.HasCitizenChaseTag[entityId] = true;
                        _cm.AddChaseTarget(entityId, entity.Id);
                        _cm.MoveCommands[entityId].Destination = entity.transform.position;
                        _cm.MoveCommands[entityId].HasNewTarget = true;
                    }
                }
            }
            else if (FieldCursor.CastToPlane(out var hitPoint))
            {
                foreach (var entityId in _entityIds)
                {
                    if (!_cm.HasCitizenTag[entityId]) continue;
                    ClearCitizenStateTags(entityId);
                    _cm.HasCitizenMoveTag[entityId] = true;
                    _cm.HasCitizenCommandedMoveTag[entityId] = true;
                    _cm.MoveCommands[entityId].Destination = hitPoint;
                    _cm.MoveCommands[entityId].HasNewTarget = true;
                }
            }
        }

        private void ClearCitizenStateTags(int entityId)
        {
            _cm.HasCitizenIdleTag[entityId] = false;
            _cm.HasCitizenMoveTag[entityId] = false;
            _cm.HasCitizenCommandedMoveTag[entityId] = false;
            _cm.HasCitizenChaseTag[entityId] = false;
            _cm.HasCitizenFightTag[entityId] = false;
            _cm.HasCitizenBuildTag[entityId] = false;
            _cm.HasCitizenHarvestTag[entityId] = false;
            _cm.HasCitizenReturnTag[entityId] = false;
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
