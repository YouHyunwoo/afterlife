using UnityEngine;

namespace Afterlife.Dev.Field
{
    [RequireComponent(typeof(Animator))]
    public class ObjectVisible : Moonstone.Ore.Local.Visible
    {
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        protected Transform selectionIndicatorTransform;

        [SerializeField] // 임시
        protected Vector2Int size = Vector2Int.one;

        public Vector2Int Size => size;

        protected virtual void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            var size = (Vector3)(Vector2)this.size;
            var center = transform.position;
            var leftBottom = center - size * 0.5f;
            for (int x = 0; x < this.size.x; x++)
            {
                for (int y = 0; y < this.size.y; y++)
                {
                    var cellCenter = leftBottom + new Vector3(x + 0.5f, y + 0.5f);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one);
                }
            }
        }

        protected override void OnInitialize()
        {
            TryGetComponent(out animator);

            var bodyTransform = transform.Find("Root").Find("Body");
            bodyTransform.TryGetComponent(out spriteRenderer);

            selectionIndicatorTransform = transform.Find("Indicator").Find("Selection");
        }

        public virtual void SetData<TObjectData>(TObjectData data) where TObjectData : ObjectData
        {
            size = data.Size;
        }

        public void ShowSelectionIndicator()
            => selectionIndicatorTransform.gameObject.SetActive(true);
        
        public void HideSelectionIndicator()
            => selectionIndicatorTransform.gameObject.SetActive(false);
    }
}