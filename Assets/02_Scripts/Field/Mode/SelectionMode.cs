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

        public event Action<Vector3, SelectionMode, object> OnSelected;

        private void Update()
        {
            // TODO: 마우스 왼쪽 드래그/클릭 시 유닛 선택
            // TODO: 마우스 오른쪽 드래그/클릭 시 유닛 명령
            UpdateSelection();
        }

        private void UpdateSelection()
        {
            if (Input.GetMouseButtonDown(0))
            {
                var isHit = _raycastSystem.Cast(out var hitPoint);
                if (!isHit) return;

                OnSelected?.Invoke(hitPoint, this, this);
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

        public void HandleResourceCommanded(ObjectVisible objectVisible, object sender)
        {
            if (objectVisible is not ResourceVisible resourceVisible) return;
            if (_objectVisibles.Count > 0 && _isCitizenSelected)
            {
                foreach (var visible in _objectVisibles)
                {
                    if (visible is CitizenVisible citizenVisible)
                    {
                        citizenVisible.DoCommand(CommandType.Harvest, new object[] { resourceVisible });
                    }
                }
            }
        }
    }
}