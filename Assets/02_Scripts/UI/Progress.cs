using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI
{
    public class Progress : View
    {
        [SerializeField] Transform containerTransform;
        [SerializeField] Image nodeImagePrefab;
        [SerializeField] Image edgeImagePrefab;
        [SerializeField] Sprite nodeFillSprite;
        [SerializeField] Sprite nodeSelectSprite;
        [SerializeField] Sprite nodeEmptySprite;

        public void SetProgress(int currentIndex, int totalCount)
        {
            foreach (Transform child in containerTransform) { Destroy(child.gameObject); }

            for (int i = 0; i < totalCount * 2 - 1; i++)
            {
                var isActive = i <= currentIndex * 2;
                var color = isActive ? Color.white : new Color(1, 1, 1, 0.2f);
                if (i % 2 == 0)
                {
                    var image = Instantiate(nodeImagePrefab, containerTransform);
                    image.color = color;
                    var index = i / 2;
                    if (index < currentIndex)
                    {
                        image.sprite = nodeFillSprite;
                    }
                    else if (index == currentIndex)
                    {
                        image.sprite = nodeSelectSprite;
                    }
                    else
                    {
                        image.sprite = nodeEmptySprite;
                    }
                }
                else
                {
                    Instantiate(edgeImagePrefab, containerTransform).color = color;
                }
            }
        }
    }
}