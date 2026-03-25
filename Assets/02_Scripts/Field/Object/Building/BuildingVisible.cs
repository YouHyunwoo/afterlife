using System;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildingVisible : ObjectVisible
    {
        protected Transform previewTransform;
        protected float townAreaInfluenceRadius;
        protected float baseBuildSpeed;
        protected float buildSpeed;
        protected float elapsedTime;
        protected BuildingState state;
        protected bool isBuilt;

        public float TownAreaInfluenceRadius => townAreaInfluenceRadius;

        public event Action<BuildingVisible, object> OnBuilt;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            var pos = transform.position;
            Gizmos.color = Color.white;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 1.5f, state.ToString());
            if (state == BuildingState.Building && elapsedTime < 1f)
                Gizmos.DrawCube(pos + Vector3.up * 2f + Vector3.right * (-1f + elapsedTime), new Vector3(elapsedTime * 2f, 0.2f));
#endif
        }
        protected override void OnInitialize()
        {
            base.OnInitialize();
            previewTransform = transform.Find("Root").Find("Preview");
            state = BuildingState.Building;
        }

        public override void SetData<TObjectData>(TObjectData data)
        {
            base.SetData(data);

            if (data is BuildingData buildingData)
            {
                townAreaInfluenceRadius = buildingData.TownAreaInfluenceRadius;
                buildSpeed = baseBuildSpeed = buildingData.BuildSpeed;
            }
        }

        protected virtual void Update()
        {
            if (state == BuildingState.Building)
            {
                elapsedTime += Time.deltaTime * buildSpeed;
                if (elapsedTime >= 1f)
                    FinishBuild();
            }
        }

        public virtual void FinishBuild()
        {
            state = BuildingState.Built;
            isBuilt = true;
            SetNormalMode();
            OnBuilt?.Invoke(this, this);
        }

        public void SetPreviewMode()
        {
            state = BuildingState.Preview;
            spriteRenderer.gameObject.SetActive(false);
            previewTransform.gameObject.SetActive(true);
        }

        public void SetNormalMode()
        {
            state = isBuilt ? BuildingState.Built : BuildingState.Building;
            spriteRenderer.gameObject.SetActive(true);
            previewTransform.gameObject.SetActive(false);
        }

        public void AttachCitizen()
        {
            buildSpeed += 0.2f;
        }

        public void DetachCitizen()
        {
            buildSpeed -= 0.2f;
        }
    }
}