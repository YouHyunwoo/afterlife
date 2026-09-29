using UnityEditor;
using UnityEngine;
namespace Afterlife.Modules.SceneFeatures.Editor
{
    [InitializeOnLoad]
    internal static class SceneFeaturePreviewLifecycle
    {
        static SceneFeaturePreviewLifecycle()
        {
            AssemblyReloadEvents.beforeAssemblyReload+=Clear;
            EditorApplication.quitting+=Clear;
            EditorApplication.playModeStateChanged+=state=>{if(state==PlayModeStateChange.ExitingEditMode)Clear();};
        }
        private static void Clear() { foreach(var host in UnityEngine.Object.FindObjectsByType<SceneFeatureHost>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(host.Session!=null)host.Clear(); }
    }
}
