using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class StageProgress : UI.View
    {
        [SerializeField] Transform stageProgressContainerTransform;
        [SerializeField] Image stageNodeImagePrefab;
        [SerializeField] Image stageEdgeImagePrefab;
        [SerializeField] Sprite stageNodeFillSprite;
        [SerializeField] Sprite stageNodeSelectSprite;
        [SerializeField] Sprite stageNodeEmptySprite;

        public void SetStageProgress(int currentStageIndex, int totalStageCount)
        {
            foreach (Transform child in stageProgressContainerTransform) { Destroy(child.gameObject); }

            for (int i = 0; i < totalStageCount * 2 - 1; i++)
            {
                var isActive = i <= currentStageIndex * 2;
                if (i % 2 == 0) {
                    var image = Instantiate(stageNodeImagePrefab, stageProgressContainerTransform);
                    image.color = isActive ? Color.white : new Color(1, 1, 1, 0.2f);
                    var index = i / 2;
                    if (index < currentStageIndex) {
                        image.sprite = stageNodeFillSprite;
                    } else if (index == currentStageIndex) {
                        image.sprite = stageNodeSelectSprite;
                    } else {
                        image.sprite = stageNodeEmptySprite;
                    }
                }
                else {
                    Instantiate(stageEdgeImagePrefab, stageProgressContainerTransform).color = isActive ? Color.white : new Color(1, 1, 1, 0.2f);
                }
            }
        }
    }
}