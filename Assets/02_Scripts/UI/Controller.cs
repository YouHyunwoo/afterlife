using UnityEngine;

namespace Afterlife.UI
{
    public abstract class Controller : MonoBehaviour
    {
        public virtual void SetUp() { }
        public virtual void TearDown() { }
        public virtual void Refresh() { }
    }
}