using UnityEngine;
using UnityEngine.EventSystems;

namespace Afterlife.Modules.Progression
{
    public sealed class TreePanSurface : MonoBehaviour, IBeginDragHandler, IDragHandler, IScrollHandler
    {
        public RectTransform Content;
        public UpgradeTreeScreen Screen;
        private Vector2 _last;

        public void OnBeginDrag(PointerEventData data)
        {
            Screen.HideTooltip();
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out _last);
        }
        public void OnDrag(PointerEventData data)
        {
            if (data.button != PointerEventData.InputButton.Left) return;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.pressEventCamera, out var point);
            Content.anchoredPosition += point - _last;
            _last = point;
        }
        public void OnScroll(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)transform, data.position, data.enterEventCamera, out var point);
            Screen.ZoomAt(point, data.scrollDelta.y);
        }
    }
}
