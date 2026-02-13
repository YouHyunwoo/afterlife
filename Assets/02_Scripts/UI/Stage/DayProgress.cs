using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class DayProgress : View
    {
        [SerializeField] RectTransform containerTransform;
        [SerializeField] Image nodeImagePrefab;
        [SerializeField] Image edgeImagePrefab;
        [SerializeField] Image dayNightImage;
        [SerializeField] Sprite daySprite;
        [SerializeField] Sprite nightSprite;
        [SerializeField] RectTransform currentIndicator;
        [SerializeField] RectTransform indicationContainer;

        public RectTransform RectTransform;

        bool IsExpandable;

        public void SetExpandable(bool isExpandable)
        {
            IsExpandable = isExpandable;
        }

        public void Initialize(int totalDays)
        {
            foreach (Transform child in containerTransform)
            {
                Destroy(child.gameObject);
            }

            if (totalDays <= 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(totalDays), "Max days must be greater than 0.");
            }

            GenerateNodes(totalDays + 1);
            LocateDayImage(0);
            SetDay();
        }

        void GenerateNodes(int nodeCount)
        {
            var nodeWidth = nodeImagePrefab.rectTransform.sizeDelta.x;
            var edgeWidth = edgeImagePrefab.rectTransform.sizeDelta.x;
            var totalWidth = 0f;

            for (int i = 0; i < nodeCount; i++)
            {
                if (i > 0)
                {
                    var edgeImage = Instantiate(edgeImagePrefab, containerTransform);
                    edgeImage.name = $"Edge {i + 1}";
                    totalWidth += edgeWidth;
                }
                var nodeImage = Instantiate(nodeImagePrefab, containerTransform);
                nodeImage.name = $"Node {i + 1}";
                totalWidth += nodeWidth;
            }

            containerTransform.sizeDelta = new Vector2(totalWidth, containerTransform.sizeDelta.y);

            if (IsExpandable)
            {
                RectTransform.sizeDelta = new Vector2(totalWidth, RectTransform.sizeDelta.y);
            }
            indicationContainer.offsetMin = new Vector2(nodeWidth / 2, indicationContainer.offsetMin.y);
            indicationContainer.offsetMax = new Vector2(-nodeWidth / 2, indicationContainer.offsetMax.y);
        }

        void LocateDayImage(float ratio)
        {
            var totalWidth = containerTransform.sizeDelta.x + indicationContainer.sizeDelta.x;
            var startX = 0;
            var endX = totalWidth;
            var currentX = Mathf.Lerp(startX, endX, ratio);

            if (IsExpandable)
            {
                currentIndicator.anchoredPosition = new Vector2(currentX, currentIndicator.anchoredPosition.y);
            }
            else
            {
                var viewportWidth = RectTransform.sizeDelta.x + indicationContainer.sizeDelta.x;
                var halfViewportWidth = viewportWidth / 2;
                var clampedX = Mathf.Clamp(currentX, halfViewportWidth, totalWidth - halfViewportWidth);
                containerTransform.anchoredPosition = new Vector2(-(clampedX - halfViewportWidth), containerTransform.anchoredPosition.y);
                currentIndicator.anchoredPosition = new Vector2(containerTransform.anchoredPosition.x + currentX, currentIndicator.anchoredPosition.y);
            }
        }

        public void SetRatio(float ratio) => LocateDayImage(ratio);

        public void SetDay() => dayNightImage.sprite = daySprite;
        public void SetNight() => dayNightImage.sprite = nightSprite;
    }
}