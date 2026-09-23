using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    public static class Day01BloomSetup
    {
        static Day01BloomSetup() { EditorApplication.delayCall+=Apply; }
        private static void Apply()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall+=Apply; return; }
            var renderer=AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Devlog/Youtube/Day01/Rendering/ShowcaseDepthRenderer.asset");
            if (renderer==null) return;
            var serialized=new SerializedObject(renderer);
            var property=serialized.FindProperty("postProcessData");
            if (property==null || property.objectReferenceValue!=null) return;
            var data=AssetDatabase.LoadAssetAtPath<PostProcessData>("Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset");
            if (data==null) { Debug.LogError("Devlog: URP PostProcessData missing."); return; }
            property.objectReferenceValue=data;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(renderer); AssetDatabase.SaveAssets();
            Debug.Log("Devlog Bloom: renderer PostProcessData connected.",renderer);
        }
    }
}
