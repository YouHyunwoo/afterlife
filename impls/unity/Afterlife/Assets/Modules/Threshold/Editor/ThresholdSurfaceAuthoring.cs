using System.IO;
using Afterlife.Modules.Threshold.Unity;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Editor
{
    /// <summary>Creates reusable dense meshes and local reflection lights only in the sample prefab.</summary>
    internal static class ThresholdSurfaceAuthoring
    {
        internal static void Apply()
        {
            string folder = ThresholdLabInstaller.Root + "/Meshes";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(ThresholdLabInstaller.Root, "Meshes");
            var poolMesh = Grid(folder + "/PuddleSurface.asset", 128, 192, true);
            var portalMesh = Grid(folder + "/PortalSurface.asset", 96, 128, false);
            string path = ThresholdLabInstaller.Root + "/Prefabs/VoidEnvironment.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var pool = root.transform.Find("ObsidianPuddle");
                pool.localScale = Vector3.one;
                pool.GetComponent<MeshFilter>().sharedMesh = poolMesh;
                var portal = root.GetComponentInChildren<ThresholdPortal>(true);
                portal.Surface.GetComponent<MeshFilter>().sharedMesh = portalMesh;
                AddLight(root.transform, "PuddleReflectionLight", new Vector3(.3f, 3, 1), 7, 14);
                var portalLight = root.transform.Find("PortalReflectionLight");
                if (portalLight != null) portalLight.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            var poolMaterial = AssetDatabase.LoadAssetAtPath<Material>(ThresholdLabInstaller.Root + "/Materials/Pool.mat");
            poolMaterial.SetFloat("_WaveHeight", .035f); poolMaterial.SetFloat("_WaveWidth", .12f); poolMaterial.SetFloat("_Gloss", 96);
            EditorUtility.SetDirty(poolMaterial);
            var portalMaterial = AssetDatabase.LoadAssetAtPath<Material>(ThresholdLabInstaller.Root + "/Materials/Portal.mat");
            portalMaterial.SetFloat("_Gloss", 72); EditorUtility.SetDirty(portalMaterial);
            AssetDatabase.SaveAssets();
        }
        private static void AddLight(Transform parent, string name, Vector3 position, float intensity, float range)
        {
            var child = parent.Find(name);
            if (child == null) { child = new GameObject(name, typeof(Light)).transform; child.SetParent(parent, false); }
            child.localPosition = position;
            var light = child.GetComponent<Light>(); light.type = LightType.Point; light.color = Color.white;
            light.intensity = intensity; light.range = range; light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.ForcePixel; light.cullingMask = ~(1 << 30);
        }
        private static Mesh Grid(string path, int columns, int rows, bool horizontal)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) return existing;
            var vertices = new Vector3[(columns + 1) * (rows + 1)];
            var uv = new Vector2[vertices.Length];
            var indices = new int[columns * rows * 6];
            for (int y = 0; y <= rows; y++)
                for (int x = 0; x <= columns; x++)
                {
                    int i = y * (columns + 1) + x;
                    float u = (float)x / columns, v = (float)y / rows;
                    uv[i] = new Vector2(u, v);
                    vertices[i] = horizontal ? new Vector3((u - .5f) * 6, 0, (v - .5f) * 10 - 2)
                        : new Vector3(u - .5f, v - .5f, 0);
                }
            int k = 0;
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < columns; x++)
                {
                    int a = y * (columns + 1) + x, b = a + 1, c = a + columns + 1, d = c + 1;
                    indices[k++] = a; indices[k++] = c; indices[k++] = b;
                    indices[k++] = b; indices[k++] = c; indices[k++] = d;
                }
            var mesh = new Mesh { name = Path.GetFileNameWithoutExtension(path), vertices = vertices, uv = uv, triangles = indices };
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var bounds = mesh.bounds; bounds.Expand(.6f); mesh.bounds = bounds;
            AssetDatabase.CreateAsset(mesh, path); return mesh;
        }
    }
}
