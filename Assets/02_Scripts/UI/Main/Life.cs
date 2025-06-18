using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Life : UI.View
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