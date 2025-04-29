using UnityEngine;

namespace Afterlife.View
{
    public class Life : UIView
    {
        [SerializeField] Transform lifeContainerTransform;
        [SerializeField] GameObject lifePrefab;

        public void SetLifes(int lifes)
        {
            foreach (Transform child in lifeContainerTransform) { Destroy(child.gameObject); }

            for (int i = 0; i < lifes; i++)
            {
                Instantiate(lifePrefab, lifeContainerTransform);
            }
        }
    }
}