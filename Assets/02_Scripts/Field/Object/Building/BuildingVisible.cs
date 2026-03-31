using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class BuildingVisible : ObjectVisible<Building>
    {
        protected Transform previewTransform;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

            var pos = transform.position;
            Gizmos.color = Color.white;
            var state = @object.BuildingState;
            var buildRate = @object.BuildRate;
#if UNITY_EDITOR
            UnityEditor.Handles.Label(pos + Vector3.up * 1.5f, state.ToString());
            if (state == BuildingState.Building && buildRate < 1f)
                Gizmos.DrawCube(pos + Vector3.up * 2f + Vector3.right * (-1f + buildRate), new Vector3(buildRate * 2f, 0.2f));
#endif
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            previewTransform = transform.Find("Root").Find("Preview");
        }

        public void SetMode(BuildingMode mode)
        {
            switch (mode)
            {
                case BuildingMode.Preview:
                    spriteRenderer.gameObject.SetActive(false);
                    previewTransform.gameObject.SetActive(true);
                    break;
                case BuildingMode.Normal:
                    spriteRenderer.gameObject.SetActive(true);
                    previewTransform.gameObject.SetActive(false);
                    break;
            }
        }
    }
}