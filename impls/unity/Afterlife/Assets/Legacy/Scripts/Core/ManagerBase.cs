using UnityEngine;

namespace Afterlife.Core
{
    public abstract class ManagerBase : MonoBehaviour
    {
        public virtual void SetUp() { }
        public virtual void TearDown() { }
    }
}