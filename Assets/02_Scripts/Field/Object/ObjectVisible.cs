using Afterlife.Model;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    [RequireComponent(typeof(Animator))]
    public class ObjectVisible : Moonstone.Ore.Local.Visible
    {
        [Header("Sight")]
        [SerializeField] protected Model.Light _sight;

        protected Animator animator;
        protected SpriteRenderer spriteRenderer;
        protected Transform selectionIndicatorTransform;

        private FogSystem _fogSystem;
        private bool _fogLightActive;

        protected override void OnInitialize()
        {
            TryGetComponent(out animator);

            var bodyTransform = transform.Find("Root").Find("Body");
            bodyTransform.TryGetComponent(out spriteRenderer);

            selectionIndicatorTransform = transform.Find("Indicator").Find("Selection");
        }

        public void ShowSelectionIndicator()
            => selectionIndicatorTransform.gameObject.SetActive(true);

        public void HideSelectionIndicator()
            => selectionIndicatorTransform.gameObject.SetActive(false);

        public virtual void SetFogAlpha(float fogValue)
        {
            if (spriteRenderer == null) return;
            var c = spriteRenderer.color;
            spriteRenderer.color = new Color(c.r, c.g, c.b, 1f - fogValue);
        }

        public virtual void BindFogSystem(FogSystem fogSystem)
        {
            _fogSystem = fogSystem;
        }

        public void DetachFogLight()
        {
            SetFogLightActive(false);
            _fogSystem = null;
        }

        protected void SetFogLightActive(bool active)
        {
            if (_fogSystem == null || _sight == null) return;
            if (active == _fogLightActive) return;
            _fogLightActive = active;
            if (active) _fogSystem.AddLight(transform, _sight);
            else _fogSystem.RemoveLight(_sight);
        }
    }

    public class ObjectVisible<TObject> : ObjectVisible where TObject : Object
    {
        protected TObject @object;

        public TObject Object => @object;

        public virtual void Bind(TObject @object)
            => this.@object = @object;

        protected virtual void Update() { }

        protected virtual void OnDrawGizmos()
        {
            if (@object == null) return;

            Gizmos.color = Color.yellow;
            var size = (Vector3)(Vector2)@object.Size;
            var center = transform.position;
            var leftBottom = center - size * 0.5f;
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    var cellCenter = leftBottom + new Vector3(x + 0.5f, y + 0.5f);
                    Gizmos.DrawWireCube(cellCenter, Vector3.one);
                }
            }
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.down, "체력: " + @object.Health);
#endif
        }
    }
}