using UnityEngine;

namespace Afterlife.Core
{
    public class SystemBase : MonoBehaviour
    {
        protected virtual void Awake() => enabled = false;

        public virtual void SetUp() { }
        public virtual void TearDown() { }
    }
}