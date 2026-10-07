using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class TextIndicator : View
    {
        Canvas canvas;
        public TextMeshProUGUI Text;

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>();
        }

        public void Show(Vector3 position, string text)
        {
            var screenPosition = Camera.main.WorldToScreenPoint(position);
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            Vector2 uiPosition;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPosition, canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : Camera.main, out uiPosition);
            Text.rectTransform.anchoredPosition = uiPosition;
            SetText(text);
            gameObject.SetActive(true);
            Text.rectTransform.DOAnchorPosY(Text.rectTransform.anchoredPosition.y + 50f, 1f).SetEase(Ease.Linear);
            Text.DOFade(0f, 1f).OnComplete(() => { Destroy(gameObject); });
        }

        public void SetText(string text)
        {
            if (Text != null)
            {
                Text.text = text;
            }
            else
            {
                Debug.LogError("Text component is not initialized.");
            }
        }
    }
}