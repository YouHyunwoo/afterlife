using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class DayProgress : UIView
    {
        [SerializeField] Transform containerTransform;
        [SerializeField] Image NodeImagePrefab;
        [SerializeField] Image EdgeImagePrefab;
        [SerializeField] Image DayImage;
        [SerializeField] Sprite DaySprite;
        [SerializeField] Sprite NightSprite;
        [SerializeField] RectTransform CurrentIndicator;

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

            GenerateNodes(totalDays);
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
                    var edgeImage = Instantiate(EdgeImagePrefab, containerTransform);
                    edgeImage.name = $"Edge {i + 1}";
                    totalWidth += edgeImage.rectTransform.sizeDelta.x;
                }
                var nodeImage = Instantiate(NodeImagePrefab, containerTransform);
                nodeImage.name = $"Node {i + 1}";
                totalWidth += nodeImage.rectTransform.sizeDelta.x;
            }

            RectTransform.sizeDelta = new Vector2(totalWidth, RectTransform.sizeDelta.y);
        }

        void LocateDayImage(float ratio)
        {
            var nodeWidth = NodeImagePrefab.rectTransform.sizeDelta.x;
            var totalWidth = RectTransform.sizeDelta.x;

            var startX = nodeWidth / 2;
            var endX = totalWidth - nodeWidth / 2;
            var currentX = Mathf.Lerp(startX, endX, ratio);
            CurrentIndicator.anchoredPosition = new Vector2(currentX, CurrentIndicator.anchoredPosition.y);
        }

        public void SetRatio(float ratio) => LocateDayImage(ratio);

        public void SetDay() => DayImage.sprite = DaySprite;
        public void SetNight() => DayImage.sprite = NightSprite;
    }
}