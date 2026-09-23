using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Afterlife.Devlog.Day01.Editor
{
    // 최초 임포트 때 독립 씬을 생성한다. 이미 저장된 씬과 사용자가 연 씬은 덮어쓰지 않는다.
    [InitializeOnLoad]
    public static class Day01SceneBuilder
    {
        public const string Root = "Assets/Devlog/Youtube/Day01";
        public const string ScenePath = Root + "/Scenes/Devlog_Day01.unity";
        private const string World = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/";
        private static Material _gray, _grayGround, _model, _wind, _plainModel, _ground, _path, _chips;
        private static readonly Vector3[] Trees =
        {
            new Vector3(-4.7f,0,2.8f), new Vector3(-3.1f,0,3.7f), new Vector3(-1.2f,0,4.2f),
            new Vector3(1,0,4.4f), new Vector3(4.6f,0,3.9f), new Vector3(5.3f,0,1.9f),
            new Vector3(-5.2f,0,.4f), new Vector3(-4.9f,0,-2.5f), new Vector3(4.8f,0,-2.8f)
        };
        private static readonly Vector3[] Rocks =
        {
            new Vector3(2.4f,0,-1.7f), new Vector3(3.5f,0,-1.3f), new Vector3(3,0,-2.7f),
            new Vector3(-3.1f,0,-1.5f), new Vector3(-3.7f,0,2.2f)
        };

        static Day01SceneBuilder() { EditorApplication.delayCall += CreateIfMissing; }

        public static void CreateIfMissing()
        {
            if (File.Exists(ScenePath)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall += CreateIfMissing; return; }
            if (Shader.Find("Afterlife/Devlog/ShowcaseSurface") == null) return;
            try { Create(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private static void Create()
        {
            foreach (string name in new[] { "Tree0", "Tree1", "Tree2", "Tree3", "Rock0", "Rock1", "Rock2", "Rock3", "House", "Ruins", "AbandonedAltar", "FallenLog", "Fern", "Flowers", "Pebbles", "AxeTool", "PickTool" })
                if (AssetDatabase.LoadAssetAtPath<GameObject>(World + name + ".fbx") == null) throw new InvalidOperationException("Devlog asset missing: " + name);
            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/Pipeline.asset");
            if (pipeline == null) throw new InvalidOperationException("Devlog pipeline missing.");
            EnsureFolder(Root + "/Scenes"); EnsureFolder(Root + "/Materials");
            _gray = Material("BeforeObjects", new Color(.56f,.57f,.59f), false, false);
            _grayGround = Material("BeforeGround", new Color(.35f,.37f,.39f), false, false);
            _plainModel = Material("ProcessModels", Color.white, true, false);
            _model = Material("AfterModels", Color.white, true, true);
            _wind = Material("AfterFoliage", Color.white, true, true); _wind.SetFloat("_Wind", 1);
            _ground = Material("ForestFloor", new Color(.29f,.40f,.20f), false, true); _ground.SetFloat("_Ground", 1);
            _path = Material("WarmEarth", new Color(.53f,.41f,.27f), false, true); _path.SetFloat("_Ground", 1);
            _chips = Material("HitChips", Color.white, false, true);
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.52f,.60f,.69f);
                RenderSettings.fog = false;
                var root = new GameObject("DevlogDay01");
                var showcase = root.AddComponent<Day01Showcase>();
                showcase.LayoutVersion = 1;
                var audio = root.AddComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0;
                showcase.Pipeline = pipeline; showcase.ParticleMaterial = _chips;
                showcase.Stages = new Transform[3];
                for (int stage = 0; stage < 3; stage++)
                {
                    string name = stage == 0 ? "Before_Primitives" : stage == 1 ? "Process_ModelPlacement" : "After_PixelForest";
                    Transform space = Group(root.transform, name);
                    space.localPosition = new Vector3(stage * 50, 0, 0);
                    showcase.Stages[stage] = space;
                    Populate(space, stage);
                }
                var cameraRoot = new GameObject("ShowcaseCamera"); cameraRoot.transform.SetParent(root.transform);
                var camera = cameraRoot.AddComponent<Camera>(); camera.tag = "MainCamera";
                camera.orthographic = true; camera.orthographicSize = 5.5f;
                camera.nearClipPlane = .1f; camera.farClipPlane = 60;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.075f,.105f,.12f);
                camera.allowHDR = false; camera.allowMSAA = false;
                camera.transform.position = new Vector3(110,13.5f,-16);
                camera.transform.rotation = Quaternion.LookRotation(new Vector3(-10,-13,16));
                cameraRoot.AddComponent<AudioListener>();
                var cameraData = cameraRoot.AddComponent<UniversalAdditionalCameraData>();
                cameraData.renderPostProcessing = false; cameraData.antialiasing = AntialiasingMode.None;
                showcase.ShowcaseCamera = camera;
                var lightRoot = new GameObject("WarmSun"); lightRoot.transform.SetParent(root.transform);
                var light = lightRoot.AddComponent<Light>(); light.type = LightType.Directional;
                light.color = new Color(1,.87f,.68f); light.intensity = 1.1f;
                light.shadows = LightShadows.Soft; light.shadowBias = .035f; light.shadowNormalBias = .25f;
                lightRoot.transform.rotation = Quaternion.Euler(48,-32,0);
                RenderSettings.sun = light;
                showcase.Axe = Model(root.transform, "AxeTool", Vector3.zero, 1, _model).gameObject;
                showcase.Pick = Model(root.transform, "PickTool", Vector3.zero, 1, _model).gameObject;
                showcase.Axe.SetActive(false); showcase.Pick.SetActive(false);
                Day01ContentUpgrade.ApplyTo(showcase);
                Day01WaterUpgrade.ApplyTo(showcase);
                Day01AxisMigration.ApplySnowLayout(showcase);
                Day01AxisMigration.ApplyBeachLayout(showcase);
                Day01WaterUpgrade.ApplySeparatedLayers(showcase);
                Day01ContentUpgrade.UpdatePath(showcase);
                Day01TerrainVariants.ApplyTo(showcase);
                Day01ContentUpgrade.AddPassiveClickSurfaces(showcase);
                Day01BiomeBlendUpgrade.ApplyTo(showcase);
                Day01ForestUpgrade.ApplyTo(showcase);
                if (!EditorSceneManager.SaveScene(scene, ScenePath)) throw new IOException("Devlog scene save failed.");
                AssetDatabase.SaveAssets();
                Debug.Log("Devlog Day01 created: " + ScenePath + " (Before 0 / Process 50 / After 100)");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static void Populate(Transform stage, int phase)
        {
            bool before = phase == 0;
            Transform terrain = Group(stage, "Terrain_Flat");
            // 모든 공간의 지면 윗면은 정확히 y=0이다. 흙길은 z-fighting 방지용 0.003만 띄운다.
            Primitive(terrain, "Ground", PrimitiveType.Cube, new Vector3(0,-.13f,0), new Vector3(12,.26f,10), before ? _grayGround : _ground);
            for (int i = 0; i < 12; i++)
            {
                float z = -4.4f + i * .65f;
                float x = -.35f + Mathf.Sin(i * .48f) * .65f;
                Primitive(terrain, "Path_" + i.ToString("00"), PrimitiveType.Cube, new Vector3(x,.0015f,z), new Vector3(1.55f,.003f,.82f), before ? _gray : _path);
            }
            Transform props = Group(stage, "Environment");
            if (before)
            {
                Primitive(props, "House_Blockout", PrimitiveType.Cube, new Vector3(2.35f,1,2.1f), new Vector3(2.2f,2,1.9f), _gray);
                Primitive(props, "Ruins_Blockout", PrimitiveType.Cube, new Vector3(-2.8f,.55f,.7f), new Vector3(1.5f,1.1f,1.3f), _gray);
                Primitive(props, "Altar_Blockout", PrimitiveType.Cylinder, new Vector3(.1f,.3f,2.1f), new Vector3(1,.3f,1), _gray);
                Primitive(props, "Log_Blockout", PrimitiveType.Cube, new Vector3(-2,.23f,-3.1f), new Vector3(1.8f,.46f,.5f), _gray);
            }
            else
            {
                var house = Model(props, "House", new Vector3(2.35f,0,2.1f), 1.8f, phase == 1 ? _plainModel : _model); house.Rotate(0,180,0);
                Model(props, "Ruins", new Vector3(-2.8f,0,.7f), 1.3f, phase == 1 ? _plainModel : _model);
                Model(props, "AbandonedAltar", new Vector3(.1f,0,2.1f), 1, phase == 1 ? _plainModel : _model);
                Model(props, "FallenLog", new Vector3(-2,0,-3.1f), 1.5f, phase == 1 ? _plainModel : _model).Rotate(0,30,0);
            }
            Transform resources = Group(stage, "ClickableResources");
            for (int i = 0; i < Trees.Length; i++) Resource(resources, "Tree" + i % 4, Trees[i], 1.15f + i % 3 * .13f, phase, true, i * 37);
            for (int i = 0; i < Rocks.Length; i++) Resource(resources, "Rock" + i % 4, Rocks[i], 1.05f + i % 3 * .15f, phase, false, i * 53);
            Transform detail = Group(stage, "GroundDetails");
            var random = new System.Random(1901);
            for (int i = 0; i < 48; i++)
            {
                float x = (float)random.NextDouble() * 10.6f - 5.3f;
                float z = (float)random.NextDouble() * 8.4f - 4.2f;
                if (Mathf.Abs(x) < 1.3f || (x > 1 && x < 3.7f && z > .8f)) continue;
                float size = .6f + (float)random.NextDouble() * .65f;
                if (before)
                    Primitive(detail, "Detail_Blockout_" + i, PrimitiveType.Cube, new Vector3(x,.08f,z), new Vector3(.28f,.16f,.28f)*size, _gray);
                else
                    Model(detail, i % 7 == 0 ? "Flowers" : i % 3 == 0 ? "Pebbles" : "Fern", new Vector3(x,0,z), size, phase == 1 ? _plainModel : _wind).Rotate(0,i*71,0);
            }
            Group(stage, "AdditionalTakeSpace").localPosition = new Vector3(0,0,14);
        }

        private static void Resource(Transform parent, string asset, Vector3 position, float scale, int phase, bool wood, float yaw)
        {
            Transform root = Group(parent, asset + "_" + parent.childCount.ToString("00"));
            root.localPosition = position;
            Transform visual;
            if (phase == 0)
            {
                visual = Group(root, "PrimitiveVisual");
                if (wood)
                {
                    Primitive(visual, "Trunk", PrimitiveType.Cylinder, new Vector3(0,.6f,0), new Vector3(.24f,.6f,.24f) * scale, _gray);
                    Primitive(visual, "Canopy", PrimitiveType.Sphere, new Vector3(0,1.5f,0), new Vector3(1.1f,1.4f,1.1f) * scale, _gray);
                }
                else Primitive(visual, "Rock", PrimitiveType.Cube, new Vector3(0,.32f,0), new Vector3(.85f,.64f,.75f) * scale, _gray);
            }
            else visual = Model(root, asset, Vector3.zero, scale, phase == 1 ? _plainModel : wood ? _wind : _model);
            visual.localRotation *= Quaternion.Euler(0,yaw,0);
            // 렌더러 실제 bounds로 클릭 영역을 만든다. primitive 자식 콜라이더는 제거한다.
            foreach (var collider in root.GetComponentsInChildren<Collider>()) UnityEngine.Object.DestroyImmediate(collider);
            var renderers = visual.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            var box = root.gameObject.AddComponent<BoxCollider>();
            box.center = root.InverseTransformPoint(bounds.center); box.size = bounds.size;
            var target = root.gameObject.AddComponent<ShowcaseTarget>(); target.Visual = visual; target.Wood = wood; target.Juicy = phase == 2;
        }

        private static Transform Model(Transform parent, string name, Vector3 position, float scale, Material material)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(World + name + ".fbx");
            // FBX 루트의 Blender → Unity 축 보정은 보존한다.
            // 배치 방향과 타격 모션은 Unity Y-up 좌표계의 별도 피벗에만 적용한다.
            var pivot = Group(parent, name + "_Pivot");
            pivot.localPosition = position;
            pivot.localScale = Vector3.one * scale;
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset, pivot);
            instance.name = name;
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = asset.transform.localRotation;
            instance.transform.localScale = asset.transform.localScale;
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var materials = new Material[renderer.sharedMaterials.Length];
                for (int i = 0; i < materials.Length; i++) materials[i] = material;
                renderer.sharedMaterials = materials;
            }
            return pivot;
        }

        private static Transform Group(Transform parent, string name)
        { var go = new GameObject(name); go.transform.SetParent(parent, false); return go.transform; }

        private static void Primitive(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 size, Material material)
        {
            var go = GameObject.CreatePrimitive(type); go.name = name; go.transform.SetParent(parent,false);
            go.transform.localPosition = position; go.transform.localScale = size; go.GetComponent<Renderer>().sharedMaterial = material;
            // 지면을 제외한 환경 장식은 클릭 레이를 가리지 않는다.
            if (name != "Ground") UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }

        private static Material Material(string name, Color color, bool palette, bool stylized)
        {
            string path = Root + "/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")) { name = name };
            material.SetColor("_BaseColor",color); material.SetFloat("_VertexPalette",palette ? 1 : 0); material.SetFloat("_Stylized",stylized ? 1 : 0);
            AssetDatabase.CreateAsset(material,path); return material;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\','/'); EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
