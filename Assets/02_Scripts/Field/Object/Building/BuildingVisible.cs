using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildingVisible : ObjectVisible
    {
        protected Transform previewTransform;
        protected float townAreaInfluenceRadius;

        public float TownAreaInfluenceRadius => townAreaInfluenceRadius;

        protected override void OnInitialize()
        {
            base.OnInitialize();
            previewTransform = transform.Find("Root").Find("Preview");
        }

        public override void SetData<TObjectData>(TObjectData data)
        {
            base.SetData(data);

            if (data is BuildingData buildingData)
            {
                townAreaInfluenceRadius = buildingData.TownAreaInfluenceRadius;
            }
        }

        public void SetPreviewMode(bool isPreviewMode)
        {
            spriteRenderer.gameObject.SetActive(isPreviewMode);
            previewTransform.gameObject.SetActive(isPreviewMode);
        }
    }
}