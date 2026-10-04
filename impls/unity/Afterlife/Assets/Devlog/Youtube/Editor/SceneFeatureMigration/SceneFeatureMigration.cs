using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.SceneFeatures.Editor;
using Afterlife.Modules.Interaction;
using Afterlife.Modules.Terrain;
using Afterlife.Devlog.Day01;
using Afterlife.Devlog.Day02;
namespace Afterlife.Devlog.Editor
{
    // 명시적인 요청 파일이 있을 때만 실행한다. 도메인 재로드만으로 씬을 변경하지 않는다.
    [InitializeOnLoad]
    internal static class SceneFeatureMigration
    {
        private const string Request="Temp/scene-feature-migration.request";
        private const string Report="Temp/scene-feature-migration.result.txt";
        private const string Shared="Assets/Afterlife/Modules/SceneFeatures/Presets";
        private const string Day01="Assets/Devlog/Youtube/Day01";
        private const string Day02="Assets/Devlog/Youtube/Day02/Scenes/Devlog_Day02.unity";
        static SceneFeatureMigration(){EditorApplication.update+=Poll;}
        private static void Poll()
        {
            if(!File.Exists(Request)||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)return;
            string command;
            // The external writer may still hold the file for a few milliseconds.
            try { command=File.ReadAllText(Request).Trim();if(command.Length==0)return;File.Delete(Request); }
            catch(IOException) { return; }
            if(command=="refresh"){AssetDatabase.Refresh();File.WriteAllText(Report,"REFRESHED: explicit asset import request.");return;}
            if(command=="day02-hold") {
                try {
                    var type=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Afterlife.Devlog.Day02.Editor.Day02Setup")).First(t=>t!=null);
                    File.WriteAllText(Report,(string)type.GetMethod("InstallHoldFilm").Invoke(null,null));
                } catch(Exception ex){File.WriteAllText(Report,"FAILED: "+ex);Debug.LogException(ex);}
                return;
            }
            if(command=="day02-scale-report"){File.WriteAllText(Report,Day02IslandArtMigration.ReportScale());return;}
            if(command=="day02-capture") {
                try { File.WriteAllText(Report,Day02IslandArtMigration.CaptureViews()); }
                catch(Exception ex){File.WriteAllText(Report,"FAILED: "+ex);Debug.LogException(ex);}
                return;
            }
            if(command=="day02-scales") {
                try { string result=Day02IslandArtMigration.ApplyDay01Scale();AssetDatabase.SaveAssets();File.WriteAllText(Report,result); }
                catch(Exception ex){File.WriteAllText(Report,"FAILED: "+ex);Debug.LogException(ex);}
                return;
            }
            if(command=="day02-islands") {
                try { string result=Day02IslandArtMigration.Install();AssetDatabase.SaveAssets();File.WriteAllText(Report,result); }
                catch(Exception ex){File.WriteAllText(Report,"FAILED: "+ex);Debug.LogException(ex);}
                return;
            }
            try{Install();AssetDatabase.SaveAssets();File.WriteAllText(Report,"SUCCESS: profiles, Day01 adapter, Day02 first island saved. Play/Test Runner not run.");Debug.Log("Scene feature migration completed.");}
            catch(Exception ex){File.WriteAllText(Report,"FAILED: "+ex);Debug.LogException(ex);}
        }
        private static T Load<T>(string path) where T:UnityEngine.Object=>AssetDatabase.LoadAssetAtPath<T>(path);
        private static void Promote(string from,string to)
        {
            if(AssetDatabase.LoadMainAssetAtPath(from)==null || AssetDatabase.LoadMainAssetAtPath(to)!=null)return;
            Directory.CreateDirectory(Path.GetDirectoryName(to));AssetDatabase.Refresh();string error=AssetDatabase.MoveAsset(from,to);if(!string.IsNullOrEmpty(error))throw new IOException(error);
        }
        private static void Install()
        {
            foreach(var path in new[]{Day02}.Concat(AssetDatabase.FindAssets("t:Scene",new[]{Day01+"/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))) {
                var loaded=SceneManager.GetSceneByPath(path);if(loaded.isLoaded&&loaded.isDirty)throw new InvalidOperationException("먼저 씬의 기존 변경을 저장하세요: "+path);
            }
            Directory.CreateDirectory(Shared);AssetDatabase.Refresh();
            // GUID를 보존하여 기존 소비자의 참조를 유지한다.
            Promote("Assets/Devlog/Youtube/Modules/Rendering", "Assets/Afterlife/Modules/SurfaceRendering");
            Promote(Day01+"/Prefabs/HitChip.prefab",Shared+"/HitChip.prefab");
            Promote(Day01+"/Prefabs/ChimneySmoke.prefab",Shared+"/ChimneySmoke.prefab");
            var profile=Load<SceneFeatureProfile>(Shared+"/After.asset");
            if(profile==null){profile=ScriptableObject.CreateInstance<SceneFeatureProfile>();AssetDatabase.CreateAsset(profile,Shared+"/After.asset");}
            profile.Settings=SceneFeatureProfile.AfterSettings();
            var source=Load<Material>(Day01+"/Materials/AfterBlendedTerrain.mat");if(source==null)throw new InvalidOperationException("After terrain material missing");
            var ground=Load<Material>(Shared+"/AfterGround.mat");if(ground==null){ground=new Material(source);AssetDatabase.CreateAsset(ground,Shared+"/AfterGround.mat");}
            ground.SetFloat("_TerrainBlend",0);ground.SetFloat("_Grid",0);ground.SetFloat("_Stylized",.25f);profile.TerrainMaterial=ground;
            profile.WaterShader=Shader.Find("Afterlife/Water/WaterSurface");profile.GrassShader=Shader.Find("Afterlife/Vegetation/Grass");profile.LeafShader=Shader.Find("Afterlife/Vegetation/LeafCards");
            profile.ChipPrefab=Load<GameObject>(Shared+"/HitChip.prefab");profile.SmokePrefab=Load<GameObject>(Shared+"/ChimneySmoke.prefab")?.GetComponent<ParticleSystem>();
            profile.GrassSounds=new[]{Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_02"),Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_03"),Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_04")};profile.Bird=Resources.Load<AudioClip>("AmbientSounds/bird");
            foreach(var scenePath in AssetDatabase.FindAssets("t:Scene",new[]{Day01+"/Scenes"}).Select(AssetDatabase.GUIDToAssetPath))WithScene(scenePath,scene=> {
                foreach(var showcase in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day01Showcase>(true))) {
                    profile.Settings.PixelHeight=showcase.PixelHeight;profile.Settings.CursorRadius=showcase.CursorVisionRadius;profile.Settings.SourceRadius=showcase.HouseVisionRadius;profile.Settings.VisionHeight=showcase.VisionHeight;profile.Settings.TrailSeconds=showcase.VisionTrailSeconds;
                    profile.AudioBank=showcase.AudioBank;profile.ParticleMaterial=showcase.ParticleMaterial;profile.Pipeline=showcase.Pipeline;profile.Destruction=showcase.DestructionSettings;
                    showcase.FeatureProfile=profile;
                    // Day01의 V/필름 시야 조작은 유지한다.
                    showcase.FeatureOverrides.Features=new[]{new FeatureOverride{Feature=SceneFeature.Vision,Enabled=true},new FeatureOverride{Feature=SceneFeature.VisionBlur,Enabled=true}};
                    EditorUtility.SetDirty(showcase);
                }
            });
            if(profile.AudioBank!=null) {
                var bank=Load<Afterlife.Modules.WorldAudio.WorldAudioBank>(Shared+"/AfterAudio.asset");
                if(bank==null){bank=UnityEngine.Object.Instantiate(profile.AudioBank);AssetDatabase.CreateAsset(bank,Shared+"/AfterAudio.asset");}
                bank.WaterRipple=Resources.Load<AudioClip>("WorldAudio/freesori_water_ripple");bank.WaterRippleVariants=Resources.LoadAll<AudioClip>("WorldAudio/WaterRipples");profile.AudioBank=bank;EditorUtility.SetDirty(bank);
            }
            EditorUtility.SetDirty(profile);EditorUtility.SetDirty(ground);
            WithScene(Day02,scene=> {
                var view=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day02Showcase>(true)).Single();var stage=view.Stages[0];
                var host=stage.GetComponent<SceneFeatureHost>();bool first=host==null;if(first)host=stage.gameObject.AddComponent<SceneFeatureHost>();
                host.Clear();
                var resource=stage.GetComponentsInChildren<Day02Resource>(true).First(r=>!r.Stone);
                if(first) {
                    var archive=new GameObject("LegacyFirstIsland");archive.transform.SetParent(stage,false);
                    foreach(var child in stage.Cast<Transform>().ToArray())if(child!=resource.transform&&child!=archive.transform)child.SetParent(archive.transform,true);
                    archive.SetActive(false);
                    foreach(var filter in resource.GetComponentsInChildren<MeshFilter>(true))if(filter.sharedMesh!=null&&filter.sharedMesh.name.Contains("Leaf"))filter.gameObject.SetActive(false);
                }
                var oldEnvironment=stage.GetComponent<Day02IslandEnvironment>();if(oldEnvironment!=null)UnityEngine.Object.DestroyImmediate(oldEnvironment);
                var target=resource.GetComponent<FeedbackTarget>();if(target==null)target=resource.gameObject.AddComponent<FeedbackTarget>();
                target.Visual=resource.Art;target.Wood=true;target.Juicy=true;target.ExternalTick=true;
                var pickSurfaces=new List<PickSurface>();var foliage=new List<MeshFilter>();
                foreach(var filter in resource.Art.GetComponentsInChildren<MeshFilter>()) {
                    if(filter.sharedMesh==null||!filter.sharedMesh.isReadable)continue;
                    var pick=filter.GetComponent<PickSurface>();if(pick==null)pick=filter.gameObject.AddComponent<PickSurface>();pick.VisibleRenderer=filter.GetComponent<Renderer>();pick.Target=target;pickSurfaces.Add(pick);foliage.Add(filter);
                }
                host.Profile=profile;host.ActiveEnvironment=true;host.ExternalTick=true;
                host.Overrides=new SceneFeatureOverrides{OverrideMap=true,OverrideVegetation=true};
                host.Overrides.Values.Map=new MapGenerationSettings();host.Overrides.Values.OpenSeaFoam=.025f;host.Overrides.Values.GrassSamples=7;host.Overrides.Values.LeafCards=1800;
                host.Overrides.Features=new[]{new FeatureOverride{Feature=SceneFeature.Vision,Enabled=false},new FeatureOverride{Feature=SceneFeature.VisionBlur,Enabled=false},new FeatureOverride{Feature=SceneFeature.Pixels,Enabled=false},new FeatureOverride{Feature=SceneFeature.Smoke,Enabled=false},new FeatureOverride{Feature=SceneFeature.Decorations,Enabled=false},new FeatureOverride{Feature=SceneFeature.Snow,Enabled=false}};
                host.Bindings=new SceneBindings{Camera=view.ShowcaseCamera,Sun=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).FirstOrDefault(l=>l.type==LightType.Directional),Targets=new[]{target},FoliageSurfaces=foliage.ToArray(),PickSurfaces=pickSurfaces.ToArray()};
                // 도구는 공용 접촉 지점을 명시적으로 연결한다.
                host.Bindings.Axe=resource.Tool;if(resource.Tool!=null)host.Bindings.AxeContact=resource.Tool.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.StartsWith("ImpactPoint"));
                host.Apply();if(host.Session.Geometry!=null){var pos=resource.transform.localPosition;pos.y=host.Session.Geometry.Height(pos.x,pos.z);resource.transform.localPosition=pos;}
                SceneFeatureHostEditor.SavePreview(host,"Assets/Devlog/Youtube/Day02/Geometry/SceneFeatures");
                EditorUtility.SetDirty(host);EditorUtility.SetDirty(resource);EditorUtility.SetDirty(target);

            });
            AssetDatabase.SaveAssets();
            foreach(var dependency in AssetDatabase.GetDependencies(Shared+"/After.asset",true))
                if(dependency.StartsWith("Assets/Devlog/") && new[]{".mat",".asset",".shader",".prefab"}.Contains(Path.GetExtension(dependency)))
                    Promote(dependency,Shared+"/Dependencies/"+Path.GetFileName(dependency));
            AssetDatabase.SaveAssets();Selection.activeObject=profile;
        }
        private static void WithScene(string path,Action<Scene> action)
        {
            var scene=SceneManager.GetSceneByPath(path);bool opened=!scene.isLoaded;if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
            try{action(scene);EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);}finally{if(opened)EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
