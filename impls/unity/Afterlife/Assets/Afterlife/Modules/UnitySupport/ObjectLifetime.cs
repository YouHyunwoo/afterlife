using UnityEngine;
namespace Afterlife.Modules.UnitySupport
{
    public static class ObjectLifetime
    {
        public static void Release(Object value) { if(value==null)return;if(Application.isPlaying)Object.Destroy(value);else Object.DestroyImmediate(value); }
    }
}
