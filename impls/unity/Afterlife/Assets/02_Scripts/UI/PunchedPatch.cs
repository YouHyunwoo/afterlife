using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI
{
    public class PunchedPatch : View
    {
        [SerializeField] Image patchImage;
        [SerializeField] Image backgroundImage;

        public void SetPatch(Sprite patchSprite)
        {
            patchImage.sprite = patchSprite;
            patchImage.SetNativeSize();
        }

        public void SetPosition(Vector2 position)
        {
            patchImage.rectTransform.anchoredPosition = position;
        }

        public void SetSize(Vector2 size)
        {
            patchImage.rectTransform.sizeDelta = size;
        }

        public void SetRectTransform(RectTransform rectTransform)
        {
            patchImage.rectTransform.pivot = rectTransform.pivot;
            patchImage.rectTransform.anchorMin = rectTransform.anchorMin;
            patchImage.rectTransform.anchorMax = rectTransform.anchorMax;
            patchImage.rectTransform.anchoredPosition = rectTransform.anchoredPosition;
            patchImage.rectTransform.sizeDelta = rectTransform.sizeDelta;
            patchImage.rectTransform.localScale = rectTransform.localScale;
        }
    }
}