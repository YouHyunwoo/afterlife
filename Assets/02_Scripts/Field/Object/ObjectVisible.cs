using UnityEngine;

namespace Afterlife.Dev.Field
{
    // * 오브젝트 Visible
    // 1. 오브젝트 스프라이트 표시
    // 2. 오브젝트 스프라이트 애니메이션 재생
    // 3. 선택 표시자 표시/숨김
    [RequireComponent(typeof(Animator))]
    public class ObjectVisible : Moonstone.Ore.Local.Visible, IDamageable
    {
        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        protected Transform selectionIndicatorTransform;

        // TODO: 모델과 뷰로 분리
        [SerializeField]
        private ObjectData data;
        protected Vector2Int size = Vector2Int.one;
        [SerializeField]
        protected float health, maxHealth;

        public Vector2Int Size => size;
        public float Health => health;
        public float MaxHealth => maxHealth;
        public bool IsAlive => health <= 0;

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
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.down, "체력: " + health);
#endif
        }

        protected override void OnInitialize()
        {
            TryGetComponent(out animator);

            var bodyTransform = transform.Find("Root").Find("Body");
            bodyTransform.TryGetComponent(out spriteRenderer);

            selectionIndicatorTransform = transform.Find("Indicator").Find("Selection");

            if (data != null)
                SetData(data);
        }

        public void ShowSelectionIndicator()
            => selectionIndicatorTransform.gameObject.SetActive(true);
        
        public void HideSelectionIndicator()
            => selectionIndicatorTransform.gameObject.SetActive(false);

        public virtual void SetData(ObjectData data)
        {
            size = data.Size;
        }

        public void TakeDamage(float damage, Object attacker)
        {
            health -= damage;
            if (health <= 0f)
                Die(attacker);
        }

        protected virtual void Die(Object attacker)
        {
            Destroy(gameObject);
        }
    }
}