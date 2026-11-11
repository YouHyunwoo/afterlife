using Afterlife.Modules.Threshold.Unity;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Editor
{
    internal static class ThresholdLabWorld
    {
        internal static GameObject Build()
        {
            var root = new GameObject("VoidEnvironment");
            var pool = ThresholdLabInstaller.Material("Pool", "Afterlife/Threshold/Pool", Color.black);
            Shape(root.transform, "ObsidianPuddle", PrimitiveType.Plane, Vector3.zero, new Vector3(3, 1, 4), pool);
            var outline = Mat("DarkStone", new Color(.006f, .009f, .012f));
            Shape(root.transform, "DistantSilhouetteLeft", PrimitiveType.Cube, new Vector3(-5, .45f, 4), new Vector3(1.2f, .9f, 2.8f), outline);
            Shape(root.transform, "DistantSilhouetteRight", PrimitiveType.Cube, new Vector3(5, .7f, 8), new Vector3(1.4f, 1.4f, 2), outline);
            var portalObject = new GameObject("FracturedWindow"); portalObject.transform.SetParent(root.transform, false);
            var portalMaterial = ThresholdLabInstaller.Material("Portal", "Afterlife/Threshold/Portal", Color.white);
            var surface = Shape(portalObject.transform, "VillageGlass", PrimitiveType.Quad, new Vector3(.3f, 2.1f, 3), new Vector3(2.7f, 3.9f, 1), portalMaterial);
            var reflected = Shape(portalObject.transform, "WindowReflection", PrimitiveType.Quad, new Vector3(.3f, .014f, 1.4f), new Vector3(2.7f, 3.9f, 1), portalMaterial);
            reflected.transform.rotation = Quaternion.Euler(90, 0, 0);
            var portal = portalObject.AddComponent<ThresholdPortal>(); portal.Surface = surface.GetComponent<Renderer>(); portal.Reflection = reflected.GetComponent<Renderer>(); portal.Template = portalMaterial;

            var village = new GameObject("VillagePreviewOnly"); village.transform.SetParent(root.transform, false); village.transform.position = new Vector3(100, 0, 0);
            BuildVillage(village.transform);
            SetLayer(village.transform, 30);
            var cameraObject = new GameObject("VillageCamera", typeof(Camera)); cameraObject.transform.SetParent(portalObject.transform, false);
            var camera = cameraObject.GetComponent<Camera>(); camera.transform.position = new Vector3(108, 6, -11); camera.transform.LookAt(new Vector3(100, 1, 0));
            camera.fieldOfView = 38; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .075f, .1f);
            camera.cullingMask = 1 << 30; camera.depth = -5; camera.nearClipPlane = .1f; camera.farClipPlane = 45; portal.SourceCamera = camera;

            var empty = new GameObject("EmptyTrialStage"); empty.transform.SetParent(root.transform, false);
            Shape(empty.transform, "TrialFloor", PrimitiveType.Plane, new Vector3(30, -.05f, 0), new Vector3(2.5f, 1, 2.5f), Mat("TrialGround", new Color(.075f, .115f, .13f)));
            var grid = Mat("TrialGrid", new Color(.095f, .145f, .155f));
            for (int i = -10; i <= 10; i += 2)
            {
                Shape(empty.transform, "GridX", PrimitiveType.Cube, new Vector3(30 + i, -.038f, 0), new Vector3(.016f, .006f, 24), grid);
                Shape(empty.transform, "GridZ", PrimitiveType.Cube, new Vector3(30, -.038f, i), new Vector3(24, .006f, .016f), grid);
            }
            return root;
        }
        private static void BuildVillage(Transform root)
        {
            var ground = Mat("VillageGround", new Color(.14f, .22f, .2f));
            Shape(root, "VillageLand", PrimitiveType.Cylinder, Vector3.zero, new Vector3(12, .22f, 10), ground, true);
            var stone = Mat("VillageStone", new Color(.22f, .28f, .27f));
            var wall = Mat("VillagePlaster", new Color(.38f, .38f, .29f));
            var roof = Mat("VillageRoof", new Color(.15f, .23f, .25f));
            var glow = Mat("WindowLight", new Color(1, .64f, .24f));
            var path = Mat("VillagePath", new Color(.27f, .28f, .22f));
            Shape(root, "Path", PrimitiveType.Cube, new Vector3(0, .23f, -1), new Vector3(1.1f, .03f, 7), path, true);
            for (int i = 0; i < 5; i++)
            {
                float x = i % 2 == 0 ? -2 : 2; float z = (i / 2) * 2.3f - 2;
                var house = new GameObject("VillageHouse" + i); house.transform.SetParent(root, false); house.transform.localPosition = new Vector3(x, .25f, z);
                Shape(house.transform, "Walls", PrimitiveType.Cube, new Vector3(0, .6f, 0), new Vector3(1.5f, 1.2f, 1.4f), wall, true);
                var roofObj = Shape(house.transform, "Roof", PrimitiveType.Cube, new Vector3(0, 1.25f, 0), new Vector3(1.4f, 1.4f, 1.65f), roof, true); roofObj.transform.localRotation = Quaternion.Euler(0, 0, 45);
                Shape(house.transform, "WarmWindow", PrimitiveType.Cube, new Vector3(-.32f, .67f, -.711f), new Vector3(.28f, .36f, .02f), glow, true);
                Shape(house.transform, "Door", PrimitiveType.Cube, new Vector3(.3f, .38f, -.713f), new Vector3(.35f, .76f, .025f), stone, true);
                Shape(house.transform, "Chimney", PrimitiveType.Cube, new Vector3(.42f, 1.8f, .2f), new Vector3(.24f, .8f, .3f), stone, true);
            }
            var leaf = Mat("VillagePine", new Color(.055f, .13f, .12f));
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2 / 8; var p = new Vector3(Mathf.Cos(angle) * 5, 1, Mathf.Sin(angle) * 4.3f);
                Shape(root, "TreeTrunk", PrimitiveType.Cylinder, p, new Vector3(.18f, .8f, .18f), stone, true);
                Shape(root, "TreeCrown", PrimitiveType.Sphere, p + Vector3.up, new Vector3(1.35f, 2.3f, 1.35f), leaf, true);
            }
        }
        private static Material Mat(string name, Color color) => ThresholdLabInstaller.Material(name, "Universal Render Pipeline/Unlit", color);
        private static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, bool local = false)
        {
            var obj = GameObject.CreatePrimitive(type); obj.name = name; obj.transform.SetParent(parent, false);
            if (local) obj.transform.localPosition = position; else obj.transform.position = position;
            obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material;
            var collider = obj.GetComponent<Collider>(); if (collider != null) UnityEngine.Object.DestroyImmediate(collider);
            return obj;
        }
        private static void SetLayer(Transform root, int layer) { root.gameObject.layer = layer; foreach (Transform child in root) SetLayer(child, layer); }
    }
}
