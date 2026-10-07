using System;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Cinematics;
using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.Terrain;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Afterlife.Devlog.Day03.Editor
{
    public static class Day03Setup
    {
        private const string Root="Assets/Devlog/Youtube/Day03";
        public const string ScenePath=Root+"/Scenes/Devlog_Day03.unity";
        private static Sprite LoadGoldHudSprite()=>AssetDatabase.LoadAllAssetsAtPath("Assets/_AfterlifePrototype/Resources/UI/Icons16.png")
            .OfType<Sprite>().Single(s=>s.name=="Icons16_5");
        private static Sprite LoadBarleySprite()=>AssetDatabase.LoadAllAssetsAtPath("Assets/_AfterlifePrototype/Resources/UI/Icons16.png")
            .OfType<Sprite>().Single(s=>s.name=="Icons16_10");
        public static string UpdateHarvestFeedback()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Day03Village.prefab");
            try {
                ConfigureHarvest(root.GetComponent<Day03Showcase>());
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 tree surfaces, hit feedback, axe blade contact and wood collection saved. Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void ConfigureHarvest(Day03Showcase view)
        {
            view.WoodCollectionSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_AfterlifePrototype/Resources/UI/SceneSprites/Icon0.asset");
            if(view.WoodCollectionSprite==null)throw new InvalidOperationException("Wood collection sprite missing.");
            var resource=view.Resource;
            resource.Model=resource.transform.Find("Model");
            if(resource.Model==null)throw new InvalidOperationException("Resource model missing.");
            var target=resource.GetComponent<FeedbackTarget>();
            if(target==null)target=resource.gameObject.AddComponent<FeedbackTarget>();
            target.Visual=resource.Model;target.Wood=true;target.Juicy=true;target.ExternalTick=true;
            target.EnableSnow=false;target.DestructionEnabled=false;
            var surfaces=resource.Model.GetComponentsInChildren<MeshFilter>(true).Select(filter=>{
                if(filter.sharedMesh==null||!filter.sharedMesh.isReadable)throw new InvalidOperationException("Tree picking requires readable meshes.");
                var pick=filter.GetComponent<Afterlife.Modules.Interaction.PickSurface>();
                if(pick==null)pick=filter.gameObject.AddComponent<Afterlife.Modules.Interaction.PickSurface>();
                pick.VisibleRenderer=filter.GetComponent<Renderer>();pick.Target=target;return pick;
            }).ToArray();
            var host=view.Environment;
            if(host.Bindings.Axe==null) {
                // Copy authored tool art only; Day03 has no Day02 runtime dependency.
                var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Devlog/Youtube/Day02/Prefabs/SmallHarvestIsland.prefab")
                    .GetComponent<SceneFeatureHost>().Bindings;
                if(source.Axe==null||source.AxeContact==null)throw new InvalidOperationException("Authored harvest axe missing.");
                string contact=AnimationUtility.CalculateTransformPath(source.AxeContact,source.Axe.transform);
                var axe=Object.Instantiate(source.Axe,view.transform);axe.name="HarvestAxe";
                axe.transform.localPosition=Vector3.zero;axe.SetActive(false);
                host.Bindings.Axe=axe;host.Bindings.AxeContact=axe.transform.Find(contact);
            }
            if(host.Bindings.AxeContact==null)throw new InvalidOperationException("Axe blade contact missing.");
            host.Bindings.Targets=host.Bindings.Targets.Concat(new[]{target}).Distinct().ToArray();
            host.Bindings.PickSurfaces=host.Bindings.PickSurfaces.Concat(surfaces).Distinct().ToArray();
            foreach(var feature in new[]{SceneFeature.Picking,SceneFeature.Tools,SceneFeature.HitFeedback,SceneFeature.DamageMarks})
                host.Profile.Settings.Set(feature,true);
            EditorUtility.SetDirty(host.Profile);
        }
        public static string UpdateGoldHud()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Day03Village.prefab");
            try {
                var view=root.GetComponent<Day03Showcase>();
                view.GoldHudSprite=LoadGoldHudSprite();view.BarleyCollectionSprite=LoadBarleySprite();
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");
                AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 gold Icons16_5 and barley Icons16_10 feedback sprites saved. Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        public static string UpdateFilmPlan()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Day03Village.prefab");
            try {
                var director=root.GetComponent<Day03FilmDirector>();
                Day03FilmPlan.Apply(director.Film);EditorUtility.SetDirty(director.Film);
                var pose=director.Film.Shots[0].From;
                var camera=director.View.ShowcaseCamera;
                camera.transform.SetPositionAndRotation(director.View.Origin+pose.Focus+pose.Offset,Quaternion.LookRotation(-pose.Offset));
                camera.orthographicSize=pose.OrthographicSize;
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 13-shot / 118-second film, subtitles, fixed action cameras and initial prefab camera saved. Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        public static string UpgradeConstructionMode()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Day03Village.prefab");
            try {
                var view=root.GetComponent<Day03Showcase>();
                view.Resource=root.GetComponentsInChildren<Day03Site>(true).Single(s=>s.Target==VillageTarget.Resource);
                foreach(var actor in new[]{view.Farm,view.SecondHouse}) {
                    actor.gameObject.name=actor.Target==VillageTarget.Farm?"FarmBuilding":"SecondHouseBuilding";
                    actor.Foundation.SetActive(false);actor.Model.gameObject.SetActive(false);
                    if(actor.Crop!=null)actor.Crop.gameObject.SetActive(false);
                    actor.GetComponent<BoxCollider>().enabled=false;
                }
                ReadBuildingFootprints(view.Settings);EditorUtility.SetDirty(view.Settings);
                var film=root.GetComponent<Day03FilmDirector>().Film;film.Shots[8].AllowInteraction=true;EditorUtility.SetDirty(film);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 build mode presentation saved; no prebuilt sites; resource reference and footprints assigned. Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }
        private static void ReadBuildingFootprints(Day03Settings settings)
        {
            var farm=new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("Assets/_AfterlifePrototype/Resources/Data/Buildings/Farm.asset"));
            var house=new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("Assets/_AfterlifePrototype/Resources/Data/Buildings/House.asset"));
            settings.FarmWidth=farm.FindProperty("Stats.Width").intValue;settings.FarmDepth=farm.FindProperty("Stats.Depth").intValue;
            settings.HouseWidth=house.FindProperty("Stats.Width").intValue;settings.HouseDepth=house.FindProperty("Stats.Depth").intValue;
        }
        public static string UpdateHouseFacing()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Day03Village.prefab");
            try {
                var director=root.GetComponent<Day03FilmDirector>();
                AlignHousesToTiles(director.View);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");
                return "SUCCESS: both house fronts face tile -Z (screen lower left). Play not run.";
            } finally {PrefabUtility.UnloadPrefabContents(root);}
        }

        private static void AlignHousesToTiles(Day03Showcase view)
        {
            // Tile -Z projects to the lower left in the Day03 camera composition.
            var direction=view.transform.TransformDirection(Vector3.back);
            direction.y=0;
            foreach(var visual in new[]{view.StarterHouse,view.SecondHouse.Model}) {
                var model=visual.Find("Model");
                var front=model.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Split('.')[0]=="FrontMarker");
                if(front==null)throw new InvalidOperationException("House model FrontMarker is required.");
                var facing=front.position-model.position;facing.y=0;
                if(facing.sqrMagnitude<.000001f)throw new InvalidOperationException("House front marker has no horizontal direction.");
                visual.rotation=Quaternion.FromToRotation(facing.normalized,direction.normalized)*visual.rotation;
            }
        }

        public static void UpdatePresentation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            var root=PrefabUtility.LoadPrefabContents(Root+"/Prefabs/Day03Village.prefab");
            try {
                var view=root.GetComponent<Day03Showcase>();
                view.Environment.Bindings.Windows=view.StarterHouse.GetComponentsInChildren<Renderer>(true)
                    .Concat(view.SecondHouse.Model.GetComponentsInChildren<Renderer>(true)).ToArray();
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");
                view.Settings.HarvestLimit=3;EditorUtility.SetDirty(view.Settings);AssetDatabase.SaveAssets();
            } finally {PrefabUtility.UnloadPrefabContents(root);}
            Debug.Log("Day03 presentation references saved. Play not run.");
        }
        public static string Install()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Exit Play before authoring.");
            if(File.Exists(ScenePath))throw new InvalidOperationException("Day03 already exists; use the saved scene. Installer never overwrites a take.");
            foreach(var folder in new[]{"Scenes","Prefabs","Data","Geometry"})Directory.CreateDirectory(Root+"/"+folder);
            AssetDatabase.Refresh();
            var previous=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try {
                SceneManager.SetActiveScene(scene);
                var root=new GameObject("Day03Village");
                var cameraObject=new GameObject("ShowcaseCamera",typeof(Camera),typeof(AudioListener));
                cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=4.2f;
                var offset=new Vector3(7,10,-12);camera.transform.SetPositionAndRotation(Vector3.up*.6f+offset,Quaternion.LookRotation(-offset));
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.07f,.23f,.32f);
                var sunObject=new GameObject("Sun",typeof(Light));sunObject.transform.SetParent(root.transform,false);
                var sun=sunObject.GetComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.2f;sun.transform.rotation=Quaternion.Euler(50,-30,0);
                sun.shadows=LightShadows.Soft;
                var settings=ScriptableObject.CreateInstance<Day03Settings>();
                ReadBuildingRules(settings);settings.CreateRules().Validate();
                AssetDatabase.CreateAsset(settings,Root+"/Data/Day03Settings.asset");
                var sourceProfile=AssetDatabase.FindAssets("t:SceneFeatureProfile")
                    .Select(g=>AssetDatabase.LoadAssetAtPath<SceneFeatureProfile>(AssetDatabase.GUIDToAssetPath(g)))
                    .First(p=>p!=null&&p.TerrainMaterial!=null&&p.WaterShader!=null&&p.Settings.IsEnabled(SceneFeature.Grass));
                var profile=Object.Instantiate(sourceProfile);profile.name="Day03IslandProfile";
                foreach(SceneFeature feature in Enum.GetValues(typeof(SceneFeature)))profile.Settings.Set(feature,false);
                foreach(var feature in new[]{SceneFeature.Terrain,SceneFeature.Water,SceneFeature.Grass,SceneFeature.Coast,SceneFeature.Beach,SceneFeature.Wind,SceneFeature.DayNight,SceneFeature.Pixels})
                    profile.Settings.Set(feature,true);
                profile.Settings.GrassSamples=5;profile.Settings.OpenSeaFoam=.35f;
                AssetDatabase.CreateAsset(profile,Root+"/Data/Day03IslandProfile.asset");
                var map=ScriptableObject.CreateInstance<MapAsset>();
                map.Data=MapGenerator.Generate(new MapGenerationSettings{Radius=7,GrassRadiusSquared=7.5,BeachRadiusSquared=12.5,Beach=true});
                map.Data.UseHeightSamples=true;
                AssetDatabase.CreateAsset(map,Root+"/Data/Day03IslandMap.asset");
                var terrainObject=new GameObject("IslandEnvironment");terrainObject.transform.SetParent(root.transform,false);
                var host=terrainObject.AddComponent<SceneFeatureHost>();host.Profile=profile;host.Bindings.Map=map;host.Bindings.Camera=camera;host.Bindings.Sun=sun;
                host.ExternalTick=true;
                Day03OceanAuthoring.ExtendMap(host);
                Day03OceanAuthoring.ConfigureWaterLevel(host);
                BakeEnvironment(host);
                var material=new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface"));material.name="Day03BuildingSurface";
                AssetDatabase.CreateAsset(material,Root+"/Data/BuildingSurface.mat");
                var foundation=new Material(Shader.Find("Universal Render Pipeline/Lit"));foundation.color=new Color(.57f,.40f,.20f);
                AssetDatabase.CreateAsset(foundation,Root+"/Data/Foundation.mat");
                var cropMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));cropMaterial.color=new Color(.89f,.72f,.22f);
                AssetDatabase.CreateAsset(cropMaterial,Root+"/Data/Barley.mat");
                float Height(float x,float z)=>new Afterlife.Modules.TerrainView.TerrainGeometry(map.Data.Radius,map.Data.At,profile.Settings.Palette,
                    new CoastalTerrain(CoastShape.Rounded,profile.Settings.WaterHeight,profile.Settings.FloorHeight),
                    (a,b)=>(float)map.Data.Height(a,b)).Height(x,z);
                var house=Model("House",root.transform,new Vector3(-1,Height(-1,0),0),1.25f,material);
                var farm=Site(VillageTarget.Farm,"Farm",root.transform,new Vector3(1,Height(1,0),0),material,foundation);
                var next=Site(VillageTarget.SecondHouse,"House",root.transform,new Vector3(0,Height(0,-1),-1),material,foundation);
                var crop=new GameObject("BarleyRows");crop.transform.SetParent(farm.transform,false);farm.Crop=crop.transform;
                for(int i=0;i<12;i++){
                    var stalk=GameObject.CreatePrimitive(PrimitiveType.Cube);stalk.name="BarleyStalk";
                    stalk.transform.SetParent(crop.transform,false);stalk.transform.localPosition=new Vector3((i%4-.5f*3)*.15f,.24f,(i/4-1)*.16f);
                    stalk.transform.localScale=new Vector3(.035f,.38f,.035f);stalk.GetComponent<Renderer>().sharedMaterial=cropMaterial;
                    Object.DestroyImmediate(stalk.GetComponent<Collider>());
                }
                crop.SetActive(false);
                var windows=new GameObject("SecondHouseWindows");windows.transform.SetParent(next.transform,false);
                var glow=windows.AddComponent<Light>();glow.type=LightType.Point;glow.color=new Color(1,.65f,.2f);glow.range=1.7f;glow.intensity=1.8f;windows.transform.localPosition=new Vector3(0,.6f,0);windows.SetActive(false);
                host.Bindings.Windows=house.GetComponentsInChildren<Renderer>().Concat(next.Model.GetComponentsInChildren<Renderer>(true)).ToArray();
                var tree=Model("Tree0",root.transform,new Vector3(-.5f,Height(-.5f,1.7f),1.7f),1.6f,material);
                var resource=tree.AddComponent<Day03Site>();resource.Target=VillageTarget.Resource;
                var collider=tree.AddComponent<BoxCollider>();collider.center=Vector3.up*.8f;collider.size=new Vector3(.8f,1.6f,.8f);
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR SDF.asset");
                var film=CreateFilm(font);
                AssetDatabase.CreateAsset(film,Root+"/Data/Day03Film.asset");
                var view=root.AddComponent<Day03Showcase>();view.ShowcaseCamera=camera;view.Environment=host;view.Sun=sun;view.Settings=settings;view.Font=font;
                view.GoldHudSprite=LoadGoldHudSprite();
                view.BarleyCollectionSprite=LoadBarleySprite();
                view.Farm=farm;view.SecondHouse=next;view.Resource=resource;view.StarterHouse=house.transform;view.SecondHouseWindows=windows;
                ConfigureHarvest(view);
                var director=root.AddComponent<Day03FilmDirector>();director.View=view;director.Film=film;
                AlignHousesToTiles(view);
                PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/Day03Village.prefab");
                Object.DestroyImmediate(root);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"/Prefabs/Day03Village.prefab");
                PrefabUtility.InstantiatePrefab(prefab,scene);
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save Day03 scene.");
                AssetDatabase.SaveAssets();
                return "SUCCESS: Day03 scene, prefab, rules and 13-shot 118-second silent rehearsal film saved. Play not run.";
            } finally {
                if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
                if(scene.IsValid()&&scene.isLoaded)EditorSceneManager.CloseScene(scene,true);
            }
        }
        private static void ReadBuildingRules(Day03Settings settings)
        {
            ReadBuildingFootprints(settings);
            var farm=new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("Assets/_AfterlifePrototype/Resources/Data/Buildings/Farm.asset"));
            var house=new SerializedObject(AssetDatabase.LoadAssetAtPath<Object>("Assets/_AfterlifePrototype/Resources/Data/Buildings/House.asset"));
            settings.FarmCost=farm.FindProperty("GoldCost").intValue;settings.HouseCost=house.FindProperty("GoldCost").intValue;
            settings.FarmConstructionSeconds=farm.FindProperty("Stats.ConstructionSeconds").floatValue;
            settings.HouseConstructionSeconds=house.FindProperty("Stats.ConstructionSeconds").floatValue;
            settings.ProductionSeconds=farm.FindProperty("Stats.ProductionSeconds").floatValue;
            settings.ProductionGold=farm.FindProperty("Stats.ProductionAmount").intValue;
            settings.HouseRadius=house.FindProperty("Stats.TerritoryRadius").floatValue;
            settings.HouseSpeedBonus=house.FindProperty("Stats.ConstructionSpeedBonus").floatValue;
            // Filming only: residual is below next house price, with room for 3 harvesting works.
            settings.StartingGold=settings.FarmCost+settings.HouseCost-8;
        }
        internal static void BakeEnvironment(SceneFeatureHost host)
        {
            var session=new SceneFeatureSession(host.transform,host.Profile,host.Resolve(),host.Bindings,false);
            GameObject preview=null;
            try {
                preview=Object.Instantiate(session.GeneratedRoot,host.transform);preview.name="BakedIsland";preview.hideFlags=HideFlags.None;
                int meshIndex=0,materialIndex=0;
                foreach(var item in preview.GetComponentsInChildren<MeshFilter>(true)){
                    item.gameObject.hideFlags=HideFlags.None;
                    if(item.sharedMesh!=null&&string.IsNullOrEmpty(AssetDatabase.GetAssetPath(item.sharedMesh))){
                        var copy=Object.Instantiate(item.sharedMesh);item.sharedMesh=SaveGeometry(copy,Root+"/Geometry/Mesh_"+meshIndex+++".asset");
                    }
                }
                foreach(var renderer in preview.GetComponentsInChildren<Renderer>(true)){
                    renderer.gameObject.hideFlags=HideFlags.None;
                    renderer.sharedMaterials=renderer.sharedMaterials.Select(m=>{
                        if(m==null||!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(m)))return m;
                        return SaveGeometry(new Material(m),Root+"/Geometry/Material_"+materialIndex+++".mat");
                    }).ToArray();
                }
                var previous=host.BakedPreview;
                host.BakedPreview=preview;preview=null;
                if(previous!=null)Object.DestroyImmediate(previous);
            } catch {
                if(preview!=null)Object.DestroyImmediate(preview);
                throw;
            } finally {session.Dispose();}
        }
        private static T SaveGeometry<T>(T copy,string path) where T:Object
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing==null){AssetDatabase.CreateAsset(copy,path);return copy;}
            EditorUtility.CopySerialized(copy,existing);EditorUtility.SetDirty(existing);
            Object.DestroyImmediate(copy);return existing;
        }
        private static GameObject Model(string name,Transform parent,Vector3 position,float height,Material material)
        {
            var root=new GameObject(name+"Visual");root.transform.SetParent(parent,false);root.transform.localPosition=position;
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_AfterlifePrototype/Resources/AfterlifePrototype/World/"+name+".fbx");
            if(source==null)throw new FileNotFoundException(name+" model missing.");
            var model=Object.Instantiate(source,root.transform);model.name="Model";
            var renderers=model.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
            foreach(var renderer in renderers){bounds.Encapsulate(renderer.bounds);renderer.sharedMaterial=material;}
            float scale=height/Mathf.Max(.001f,bounds.size.y);model.transform.localScale*=scale;
            model.transform.localPosition-=new Vector3(bounds.center.x-root.transform.position.x,bounds.min.y-root.transform.position.y,bounds.center.z-root.transform.position.z)*scale;
            return root;
        }
        private static Day03Site Site(VillageTarget target,string model,Transform parent,Vector3 position,Material material,Material foundation)
        {
            var root=new GameObject(target+"Building");root.transform.SetParent(parent,false);root.transform.localPosition=position;
            var site=root.AddComponent<Day03Site>();site.Target=target;
            site.Model=Model(model,root.transform,Vector3.zero,model=="Farm"?.65f:1.25f,material).transform;
            var slab=GameObject.CreatePrimitive(PrimitiveType.Cube);slab.name="ConstructionFoundation";slab.transform.SetParent(root.transform,false);
            slab.transform.localPosition=Vector3.up*.025f;slab.transform.localScale=new Vector3(.9f,.05f,.9f);slab.GetComponent<Renderer>().sharedMaterial=foundation;
            Object.DestroyImmediate(slab.GetComponent<Collider>());site.Foundation=slab;
            slab.SetActive(false);
            var hit=root.AddComponent<BoxCollider>();hit.center=Vector3.up*.5f;hit.size=new Vector3(.95f,1,.95f);hit.enabled=false;
            site.Model.gameObject.SetActive(false);
            return site;
        }
        private static ShowcaseFilm CreateFilm(TMP_FontAsset font)
        {
            var film=ScriptableObject.CreateInstance<ShowcaseFilm>();film.SubtitleFont=font;
            Day03FilmPlan.Apply(film);
            return film;
        }
    }
    [CustomEditor(typeof(Day03Showcase))]
    public sealed class Day03ShowcaseInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Day03Village 프리팹 하나를 빈 씬에 배치합니다. F8 직접 촬영 / F9 자동 리허설. 설치는 기존 씬을 덮어쓰지 않습니다.",MessageType.Info);
            if(GUILayout.Button("Open saved Day03 scene")&&!EditorApplication.isPlayingOrWillChangePlaymode)
                if(EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())EditorSceneManager.OpenScene(Day03Setup.ScenePath);
        }
    }
}
