using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Afterlife.Devlog.Audio;
using Afterlife.Devlog.Destruction;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Afterlife.Devlog.Day01.Editor
{
    // 일회성 저작 마이그레이션. GUID 이동과 프리팹 저장은 Unity API만 사용한다.
    [InitializeOnLoad]
    public static class Day01ModuleOrganization
    {
        private const string Root="Assets/Devlog/Day01";
        private const string Modules="Assets/Devlog/Modules";
        // 이번 마이그레이션에서 폐기한 후보만 정리한다. 신규 음원은 보존한다.
        private static readonly HashSet<string> ArchivedAudioNames=new HashSet<string>
        {
            "02_tree_fall_short_processed", "02_tree_fall_short", "03_tree_fall_clean_crack_thud",
            "04_tree_fall_extended_no_thud", "dropping_rocks_impact_12",
            "stones_falling_impact_12", "stones_falling_impact_13", "stones_falling_impact_14", "stones_falling_impact_15"
        };
        private static bool _running;
        private static string ReportPath => Path.GetFullPath(Path.Combine(Application.dataPath,"../../../../devlog/records/unity-organization.json"));
        [Serializable] private sealed class Report
        {
            public string status;
            public string timestamp;
            public string[] prefabs;
            public string note;
        }
        static Day01ModuleOrganization()
        {
            EditorApplication.delayCall+=Apply;
            EditorApplication.playModeStateChanged+=s=>{ if (s==PlayModeStateChange.EnteredEditMode) EditorApplication.delayCall+=Apply; };
        }
        private static void Folder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/'); Folder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
        private static void Move(string from,string to)
        {
            if (string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(from))) return;
            if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(to))) throw new IOException("Migration destination already exists: "+to);
            Folder(Path.GetDirectoryName(to).Replace('\\','/'));
            string guid=AssetDatabase.AssetPathToGUID(from),error=AssetDatabase.MoveAsset(from,to);
            if (!string.IsNullOrEmpty(error)) throw new IOException(error);
            // 배치 이동 중 AssetDatabase의 새 경로 조회는 아직 갱신되지 않을 수 있다.
            // Unity가 이동한 메타데이터를 읽어 실제 보존 여부를 확인한다. 메타는 쓰지 않는다.
            string movedGuid=File.ReadLines(to+".meta").First(l=>l.StartsWith("guid: ")).Substring(6).Trim();
            if (guid!=movedGuid) throw new IOException("GUID changed: "+to);
        }
        private static void Asmdef(string path,string name,params string[] references)
        {
            Folder(Path.GetDirectoryName(path).Replace('\\','/'));
            File.WriteAllText(path,"{\"name\":\""+name+"\",\"references\":["+string.Join(",",references.Select(r=>"\""+r+"\""))+"],\"autoReferenced\":true}");
        }
        private static void AddReference(string path,string reference)
        {
            string content=File.ReadAllText(path);
            if (content.Contains("\""+reference+"\"")) return;
            int start=content.IndexOf('[',content.IndexOf("\"references\"",StringComparison.Ordinal));
            content=content.Insert(start+1,"\""+reference+"\","); File.WriteAllText(path,content);
        }
        public static void Apply()
        {
            if (_running || EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall+=Apply; return; }
            if (File.Exists(ReportPath) && JsonUtility.FromJson<Report>(File.ReadAllText(ReportPath)).status=="complete")
            {
                // 폴더 이동 직후에는 자식 경로 GUID 조회가 지연될 수 있다.
                // 다음 임포트에서도 비활성 음원 정리를 확인한다.
                FinalizeAudioArchive();
                return;
            }
            _running=true;
            try
            {
                if (File.Exists(Root+"/Runtime/ShowcasePickSurface.cs") || Directory.Exists(Root+"/Core") || Directory.Exists(Root+"/Resources/DevlogAudio"))
                {
                    WriteReport("moving",null,"에디터 에셋 이동 진행");
                    OrganizeAssets();
                    WriteReport("assets-moved",null,"코드 재컴파일 후 프리팹 생성 대기");
                    AssetDatabase.Refresh(); EditorApplication.delayCall+=Apply; return;
                }
                ExportPrefabs();
            }
            catch (Exception error) { WriteReport("error",null,error.ToString()); Debug.LogException(error); }
            finally { _running=false; }
        }
        private static void OrganizeAssets()
        {
            // StartAssetEditing 중 생성한 폴더는 MoveAsset의 부모로 즉시 인식되지 않는다.
            // 모든 목적지의 부모를 먼저 AssetDatabase에 등록한다.
            foreach (string folder in new[]{"Terrain","Vegetation","Lighting","Destruction","Hover","Shoreline",
                "Interaction/Runtime","Snow/Runtime","Rendering/Shaders","Water/Shaders","Lighting/Shaders",
                "Snow/Shaders","Destruction/Shaders","Hover/Shaders","Vegetation/Resources","Lighting/Resources",
                "Hover/Resources","Audio/Resources","Audio/Archive"}) Folder(Modules+"/"+folder);
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (string name in new[]{"Core","Vegetation","Lighting","Destruction","Hover","Shoreline"})
                    Move(Root+"/"+name,Modules+"/"+(name=="Core" ? "Terrain" : name)+"/Runtime");
                foreach (string file in new[]{"ShowcasePickMesh.cs","ShowcasePickSurface.cs"}) Move(Root+"/Runtime/"+file,Modules+"/Interaction/Runtime/"+file);
                foreach (string file in new[]{"ShowcaseSnowCap.cs","ShowcaseSnowErosion.cs"}) Move(Root+"/Runtime/"+file,Modules+"/Snow/Runtime/"+file);
                Asmdef(Modules+"/Interaction/Runtime/Afterlife.Devlog.Interaction.asmdef","Afterlife.Devlog.Interaction","Afterlife.Devlog.Contracts");
                Asmdef(Modules+"/Snow/Runtime/Afterlife.Devlog.Snow.asmdef","Afterlife.Devlog.Snow","Afterlife.Devlog.Contracts","Afterlife.Devlog.Interaction");
                foreach (string reference in new[]{"Afterlife.Devlog.Interaction","Afterlife.Devlog.Snow"})
                {
                    AddReference(Root+"/Runtime/Afterlife.Devlog.Day01.asmdef",reference);
                    AddReference(Root+"/Editor/Afterlife.Devlog.Day01.Editor.asmdef",reference);
                }
                var shaders=new Dictionary<string,string> {
                    {"ShowcaseSurface.shader","Rendering/Shaders"},{"ShowcaseWater.shader","Water/Shaders"},
                    {"ShowcaseLighting.hlsl","Lighting/Shaders"},{"ShowcaseSnowErosion.hlsl","Snow/Shaders"},
                    {"ShowcaseDissolve.hlsl","Destruction/Shaders"},{"ShowcaseHover.hlsl","Hover/Shaders"},
                    {"ShowcaseGrass.shader","Vegetation/Resources"},{"ShowcaseSmoke.shader","Lighting/Resources"},
                    {"ShowcaseHoverDust.shader","Hover/Resources"}};
                foreach (var shader in shaders)
                {
                    string old=Root+(shader.Key=="ShowcaseGrass.shader" || shader.Key=="ShowcaseSmoke.shader" || shader.Key=="ShowcaseHoverDust.shader" ? "/Resources/" : "/Shaders/")+shader.Key;
                    Move(old,Modules+"/"+shader.Value+"/"+shader.Key);
                }
                foreach (var file in Directory.GetFiles(Modules,"*",SearchOption.AllDirectories).Where(f=>f.EndsWith(".shader") || f.EndsWith(".hlsl")))
                {
                    string source=File.ReadAllText(file);
                    foreach (var include in shaders.Where(p=>p.Key.EndsWith(".hlsl")))
                        foreach (string old in new[]{include.Key,"../Shaders/"+include.Key})
                            source=source.Replace("#include \""+old+"\"","#include \""+Modules+"/"+include.Value+"/"+include.Key+"\"");
                    File.WriteAllText(file,source);
                }
                Move(Root+"/Resources/GrassSprites",Modules+"/Vegetation/Resources/GrassSprites");
                Move(Root+"/Resources/DevlogAudio",Modules+"/Audio/Resources/DevlogAudio");
                foreach (string file in Directory.GetFiles(Modules+"/Audio/Resources/DevlogAudio").Where(f=>f.EndsWith(".wav") || f.EndsWith(".mp3")).ToArray())
                    if (ArchivedAudioNames.Contains(Path.GetFileNameWithoutExtension(file))) Move(file.Replace('\\','/'),Modules+"/Audio/Archive/"+Path.GetFileName(file));
            }
            finally { AssetDatabase.StopAssetEditing(); }
        }

        private static void ExportPrefabs()
        {
            string scenePath=Day01SceneBuilder.ScenePath;
            if (!File.Exists(scenePath)) return;
            Scene scene=SceneManager.GetSceneByPath(scenePath);
            bool opened=!scene.IsValid() || !scene.isLoaded;
            if (!opened && scene.isDirty) { WriteReport("awaiting-scene-save",null,"사용자 미저장 씬을 보존 중. 저장 후 스크립트 재임포트 필요."); return; }
            Scene previous=SceneManager.GetActiveScene();
            if (opened) scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            try
            {
                var showcase=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Day01Showcase>(true)).First();
                string audioPath=Modules+"/Audio/Resources/DevlogAudio/ShowcaseAudioBank.asset";
                var bank=AssetDatabase.LoadAssetAtPath<ShowcaseAudioBank>(audioPath);
                if (bank==null)
                {
                    bank=ScriptableObject.CreateInstance<ShowcaseAudioBank>();
                    bank.WoodImpact=Resources.Load<AudioClip>("AfterlifePrototype/Audio/01B");
                    bank.WoodFall=Resources.Load<AudioClip>("DevlogAudio/gongu_13220749_tree_fall");
                    bank.StoneImpacts=new[]{Resources.Load<AudioClip>("DevlogAudio/10_dropping_rocks_impact_17"),Resources.Load<AudioClip>("DevlogAudio/11_dropping_rocks_impact_15")};
                    bank.StoneBreak=Resources.Load<AudioClip>("DevlogAudio/dropping_rocks_debris_03");
                    AssetDatabase.CreateAsset(bank,audioPath);
                }
                Folder(Modules+"/Destruction/Data");
                string profilePath=Modules+"/Destruction/Data/Day01Destruction.asset";
                var profile=AssetDatabase.LoadAssetAtPath<DestructionProfile>(profilePath);
                if (profile==null) { profile=ScriptableObject.CreateInstance<DestructionProfile>(); AssetDatabase.CreateAsset(profile,profilePath); }
                Undo.RecordObject(showcase,"Devlog 설정 에셋 연결"); showcase.AudioBank=bank; showcase.DestructionSettings=profile;
                foreach (var target in showcase.GetComponentsInChildren<ShowcaseTarget>(true))
                { Undo.RecordObject(target,"파괴 설정 연결"); target.DestructionSettings=profile; }
                var paths=new List<string>();
                Folder(Modules+"/Lighting/Prefabs");
                string smokePath=Modules+"/Lighting/Prefabs/ChimneySmoke.prefab";
                if (!File.Exists(smokePath))
                {
                    var smoke=new GameObject("ChimneySmoke");
                    var particles=smoke.AddComponent<ParticleSystem>();
                    ShowcaseSmoke.ConfigureTemplate(particles);
                    PrefabUtility.SaveAsPrefabAsset(smoke,smokePath);
                    UnityEngine.Object.DestroyImmediate(smoke);
                }
                showcase.SmokePrefab=AssetDatabase.LoadAssetAtPath<GameObject>(smokePath).GetComponent<ParticleSystem>();
                paths.Add(smokePath);
                string chipPath=Modules+"/Interaction/Prefabs/HitChip.prefab"; Folder(Path.GetDirectoryName(chipPath).Replace('\\','/'));
                if (!File.Exists(chipPath))
                {
                    var chip=GameObject.CreatePrimitive(PrimitiveType.Cube); chip.name="HitChip";
                    UnityEngine.Object.DestroyImmediate(chip.GetComponent<Collider>());
                    chip.GetComponent<Renderer>().sharedMaterial=showcase.ParticleMaterial;
                    PrefabUtility.SaveAsPrefabAsset(chip,chipPath); UnityEngine.Object.DestroyImmediate(chip);
                }
                showcase.ChipPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(chipPath); paths.Add(chipPath);
                void Save(GameObject source,string category,string name,bool connect)
                {
                    string path=Root+"/Prefabs/"+category+"/"+name+".prefab";
                    Folder(Path.GetDirectoryName(path).Replace('\\','/'));
                    if (!File.Exists(path))
                    {
                        if (connect && PrefabUtility.GetOutermostPrefabInstanceRoot(source)==null)
                            PrefabUtility.SaveAsPrefabAssetAndConnect(source,path,InteractionMode.AutomatedAction);
                        else PrefabUtility.SaveAsPrefabAsset(source,path);
                    }
                    paths.Add(path);
                }
                foreach (var target in showcase.Stages[2].GetComponentsInChildren<ShowcaseTarget>(true)) Save(target.gameObject,"Resources",target.name,true);
                var environment=showcase.Stages[2].Find("Environment");
                foreach (Transform child in environment) Save(child.gameObject,"Environment",child.name,true);
                foreach (var stage in showcase.Stages)
                {
                    Save(stage.Find("Terrain_Flat").gameObject,"Terrain",stage.name+"_Terrain",true);
                    Save(stage.gameObject,"Stages",stage.name,true);
                }
                Save(showcase.Axe,"Tools","Axe",false); Save(showcase.Pick,"Tools","Pick",false);
                Save(showcase.ShowcaseCamera.gameObject,"Camera","ShowcaseCamera",false);
                Save(showcase.gameObject,"Composition","Day01Showcase",true);
                EditorSceneManager.MarkSceneDirty(scene);
                if (!EditorSceneManager.SaveScene(scene)) throw new IOException("Scene save failed.");
                AssetDatabase.SaveAssets();
                WriteReport("complete",paths.ToArray(),"GUID 보존 이동 및 설정/프리팹 저장 완료. Play/GPU/청취 검증은 별도 대기.");
                Debug.Log("Devlog organization complete: "+paths.Count+" prefabs saved.",showcase);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (opened) EditorSceneManager.CloseScene(scene,true);
            }
        }
        private static void FinalizeAudioArchive()
        {
            string folder=Modules+"/Audio/Resources/DevlogAudio";
            if (!Directory.Exists(folder)) return;
            // 이전 허용 목록 방식이 잘못 옮긴 물소리를 GUID 보존 상태로 복구한다.
            string ripple="freesori_water_ripple.wav";
            string archived=Modules+"/Audio/Archive/"+ripple;
            if (File.Exists(archived) && !File.Exists(folder+"/"+ripple))
            {
                Move(archived,folder+"/"+ripple);
                Debug.Log("Devlog: restored water ripple audio to Resources.");
            }
            foreach (string file in Directory.GetFiles(folder).Where(f=>f.EndsWith(".wav") || f.EndsWith(".mp3")).ToArray())
                if (ArchivedAudioNames.Contains(Path.GetFileNameWithoutExtension(file)))
                    Move(file.Replace('\\','/'),Modules+"/Audio/Archive/"+Path.GetFileName(file));
        }
        private static void WriteReport(string status,string[] prefabs,string note)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath,JsonUtility.ToJson(new Report { status=status,timestamp=DateTime.Now.ToString("O"),prefabs=prefabs,note=note },true));
        }
    }
}
