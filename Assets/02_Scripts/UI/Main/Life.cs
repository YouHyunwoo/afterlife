using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Life : View
    {
        [SerializeField] Transform lifeContainerTransform;
        [SerializeField] RectTransform targetTransform; // 중심점으로 사용할 Transform
        [SerializeField] Soul soulPrefab;

        public void SetLifes(int lifes)
        {
            foreach (Transform child in lifeContainerTransform) { Destroy(child.gameObject); }

            for (int i = 0; i < lifes; i++)
            {
                var soul = Instantiate(soulPrefab, lifeContainerTransform);
                soul.center = targetTransform;
            }
        }
    }
}