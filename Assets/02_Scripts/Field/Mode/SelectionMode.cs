using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class SelectionMode : Mode
    {
        [SerializeField] private RaycastSystem _raycastSystem;

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
    }
}