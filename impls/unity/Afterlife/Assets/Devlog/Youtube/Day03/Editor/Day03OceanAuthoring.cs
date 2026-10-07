using System;
using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.Terrain;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day03.Editor
{
    // Extend the filming backdrop without regenerating the authored island or moving its camera.
    public static class Day03OceanAuthoring
    {
        public const int OceanRadius=16;
        public const float WaterHeight=-.24f;
        private const string PrefabPath="Assets/Devlog/Youtube/Day03/Prefabs/Day03Village.prefab";

        public static string UpdateWaterLevel()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try {
                var host=root.GetComponent<Day03Showcase>().Environment;
                ConfigureWaterLevel(host);
                Day03Setup.BakeEnvironment(host);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 water height set to "+host.Resolve().WaterHeight+"; ocean extent and land placement preserved. Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        internal static void ConfigureWaterLevel(SceneFeatureHost host)
        {
            host.Profile.Settings.WaterHeight=WaterHeight;
            host.Overrides.Values.WaterHeight=WaterHeight;
            EditorUtility.SetDirty(host.Profile);
        }

        public static string UpdateOceanExtent()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(PrefabPath);
            try {
                var host=root.GetComponent<Day03Showcase>().Environment;
                ExtendMap(host);
                Day03Setup.BakeEnvironment(host);
                PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 ocean extended to "+host.Bindings.Map.Data.Size+"x"+host.Bindings.Map.Data.Size+" tiles; island, camera and map GUID preserved. Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        internal static void ExtendMap(SceneFeatureHost host)
        {
            var asset=host.Bindings.Map;
            if(asset==null)throw new InvalidOperationException("Day03 map reference missing.");
            var source=asset.Data;
            if(source.Radius<OceanRadius) {
                var expanded=new MapData(OceanRadius){UseHeightSamples=source.UseHeightSamples};
                for(int z=-expanded.Radius;z<=expanded.Radius;z++)
                    for(int x=-expanded.Radius;x<=expanded.Radius;x++) {
                        int index=(z+expanded.Radius)*expanded.Size+x+expanded.Radius;
                        bool original=Math.Abs(x)<=source.Radius&&Math.Abs(z)<=source.Radius;
                        expanded.Tiles[index]=original?source.At(x,z):TerrainBiome.Water;
                        expanded.Heights[index]=source.Height(x,z);
                    }
                asset.Data=expanded;
                EditorUtility.SetDirty(asset);
            }
            host.Profile.Settings.Map.Radius=asset.Data.Radius;
            host.Overrides.Values.Map.Radius=asset.Data.Radius;
            EditorUtility.SetDirty(host.Profile);
        }
    }
}
