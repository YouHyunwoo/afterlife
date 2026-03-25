using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Afterlife.Dev.Field
{
    [RequireComponent(typeof(Animator))]
    public class ObjectVisible : Moonstone.Ore.Local.Visible
    {
        protected Animator animator;
        protected EventTrigger eventTrigger;
        protected SpriteRenderer spriteRenderer;
        [SerializeField] // 임시
        protected Vector2Int size = Vector2Int.one;

        public Vector2Int Size => size;

        public event Action<ObjectVisible, object> OnSelected; // 마우스 왼쪽 클릭
        public event Action<ObjectVisible, object> OnCommanded; // 마우스 오른쪽 클릭

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
            TryGetComponent(out eventTrigger);
            SetUpEventTriggers();

            var bodyTransform = transform.Find("Root").Find("Body");
            bodyTransform.TryGetComponent(out spriteRenderer);
        }

        protected virtual void SetUpEventTriggers()
        {
            var entry = new EventTrigger.Entry
            {
                eventID = EventTriggerType.PointerClick,
            };
            entry.callback.AddListener((data) => OnPointerClick((PointerEventData)data));
            eventTrigger.triggers.Add(entry);
        }

        public virtual void SetData<TObjectData>(TObjectData data) where TObjectData : ObjectData
        {
            size = data.Size;
        }

        #region Event Handler

        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"object {eventData.button} click: " + name);
            if (eventData.button == PointerEventData.InputButton.Left)
                OnSelected?.Invoke(this, this);
            else if (eventData.button == PointerEventData.InputButton.Right)
                OnCommanded?.Invoke(this, this);
        }

        #endregion
    }
}