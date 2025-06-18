using UnityEngine;

namespace Afterlife.UI
{
    public abstract class View : MonoBehaviour
    {
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
        public virtual void Toggle() => gameObject.SetActive(!gameObject.activeSelf);
    }
}