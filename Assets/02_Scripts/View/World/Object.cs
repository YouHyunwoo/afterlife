using TMPro;
using UnityEngine;

namespace Afterlife.View
{
    public class Object : MonoBehaviour
    {
        public float Health;

        TextMeshPro text;

        void Awake()
        {
            text = GetComponentInChildren<TextMeshPro>();
        }

        void Start()
        {
            UpdateValue();
        }

        void UpdateValue()
        {
            if (text == null) { return; }
            text.text = $"{Health:0}";
        }

        public virtual void Interact(Model.Player player)
        {
            Health -= player.AttackPower;
            if (Health <= 0f)
            {
                Died(player);
            }
            else
            {
                UpdateValue();
            }
        }

        public virtual void Died(Model.Player player)
        {
            Destroy(gameObject);
        }
    }
}