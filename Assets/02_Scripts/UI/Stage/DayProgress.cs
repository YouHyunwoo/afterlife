using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class DayProgress : View
    {
        [SerializeField] Transform containerTransform;
        [SerializeField] Image nodeImagePrefab;
        [SerializeField] Image edgeImagePrefab;
        [SerializeField] Image dayNightImage;
        [SerializeField] Sprite daySprite;
        [SerializeField] Sprite nightSprite;
        [SerializeField] RectTransform currentIndicator;

        public RectTransform RectTransform;

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
            var totalWidth = 0f;

            for (int i = 0; i < nodeCount; i++)
            {
                if (i > 0)
                {
                    var edgeImage = Instantiate(edgeImagePrefab, containerTransform);
                    edgeImage.name = $"Edge {i + 1}";
                    totalWidth += edgeImage.rectTransform.sizeDelta.x;
                }
                var nodeImage = Instantiate(nodeImagePrefab, containerTransform);
                nodeImage.name = $"Node {i + 1}";
                totalWidth += nodeImage.rectTransform.sizeDelta.x;
            }

            RectTransform.sizeDelta = new Vector2(totalWidth, RectTransform.sizeDelta.y);
        }

        void LocateDayImage(float ratio)
        {
            var nodeWidth = nodeImagePrefab.rectTransform.sizeDelta.x;
            var totalWidth = RectTransform.sizeDelta.x;

            var startX = nodeWidth / 2;
            var endX = totalWidth - nodeWidth / 2;
            var currentX = Mathf.Lerp(startX, endX, ratio);
            currentIndicator.anchoredPosition = new Vector2(currentX, currentIndicator.anchoredPosition.y);
        }

        public void SetRatio(float ratio) => LocateDayImage(ratio);

        public void SetDay() => dayNightImage.sprite = daySprite;
        public void SetNight() => dayNightImage.sprite = nightSprite;
    }
}