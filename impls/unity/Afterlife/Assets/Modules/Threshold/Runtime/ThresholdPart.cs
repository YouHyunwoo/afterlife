using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Prefab component membership. The parent host owns the session, never a global singleton.</summary>
    public abstract class ThresholdPart : MonoBehaviour
    {
        internal ThresholdHost Host { get; private set; }
        protected virtual void OnEnable()
        {
            Host = GetComponentInParent<ThresholdHost>();
            if (Host != null) Host.Register(this);
        }
        protected virtual void OnDisable()
        {
            if (Host != null) Host.Unregister(this);
            Host = null;
        }
    }
}
