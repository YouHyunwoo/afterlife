using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    internal static class Day01HouseVisionSetup
    {
        static Day01HouseVisionSetup() { EditorApplication.delayCall+=Apply; }
        private static void Apply()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall+=Apply; return; }
            const string path=Day01SceneBuilder.Root+"/Prefabs/Composition/Day01Showcase.prefab";
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (asset==null || !Mathf.Approximately(asset.GetComponent<Day01Showcase>().HouseVisionRadius,4.2f)) return;
            var root=PrefabUtility.LoadPrefabContents(path);
            try
            {
                root.GetComponent<Day01Showcase>().HouseVisionRadius=3f;
                PrefabUtility.SaveAsPrefabAsset(root,path);
                Debug.Log("Devlog house vision radius saved: 4.2m -> 3m.");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
}
