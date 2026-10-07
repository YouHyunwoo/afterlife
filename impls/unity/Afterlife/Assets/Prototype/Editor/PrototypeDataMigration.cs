using System;
using System.IO;
using System.Linq;
using Afterlife.Modules.Progression;
using UnityEditor;
using UnityEngine;
namespace Afterlife.Prototype.Editor
{
    // 기존 에셋 GUID/개별 수정값을 유지한다. 새 에셋과 .meta 생성은 AssetDatabase에만 맡긴다.
    public static class PrototypeDataMigration
    {
        public static PrototypeSettings CreateSettingsCopy(PrototypeSettings source,string path,Action<PrototypeSettings> configure)
        {
            if(File.Exists(path))throw new IOException("설정 에셋이 이미 있습니다: "+path);
            string folder=Path.GetDirectoryName(path).Replace('\\','/')+"/"+Path.GetFileNameWithoutExtension(path)+"Data";
            EnsureFolder(folder);
            var copy=UnityEngine.Object.Instantiate(source);
            foreach(var field in typeof(PrototypeSettings).GetFields())
            {
                if(!typeof(ScriptableObject).IsAssignableFrom(field.FieldType))continue;
                var child=field.GetValue(source) as ScriptableObject;if(child==null)continue;
                string childPath=folder+"/"+field.Name+".asset";
                if(File.Exists(childPath))throw new IOException("복사 대상 데이터가 이미 있습니다: "+childPath);
                var cloned=UnityEngine.Object.Instantiate(child);AssetDatabase.CreateAsset(cloned,childPath);field.SetValue(copy,cloned);
            }
            configure?.Invoke(copy);
            foreach(var field in typeof(PrototypeSettings).GetFields())if(field.GetValue(copy) is ScriptableObject child)Save(child);
            AssetDatabase.CreateAsset(copy,path);Save(copy);return copy;
        }
        [InitializeOnLoadMethod]
        private static void Schedule() { EditorApplication.delayCall+=RunWhenReady; }
        private static void RunWhenReady()
        {
            if(EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode)
            { EditorApplication.delayCall+=RunWhenReady;return; }
            Migrate();
        }
        [MenuItem("Assets/Afterlife/데이터 분리 및 검사")]
        public static void Migrate()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)return;
            try
            {
                foreach(string guid in AssetDatabase.FindAssets("t:PrototypeSettings",new[]{"Assets"}))
                {
                    string path=AssetDatabase.GUIDToAssetPath(guid);
                    var settings=AssetDatabase.LoadAssetAtPath<PrototypeSettings>(path);
                    if(settings.DataVersion>=PrototypeSettings.CurrentDataVersion)continue;
                    MigrateSettings(settings,path);
                }
                foreach(string guid in AssetDatabase.FindAssets("t:PrototypeBuildingData",new[]{"Assets"}))
                {
                    var building=AssetDatabase.LoadAssetAtPath<PrototypeBuildingData>(AssetDatabase.GUIDToAssetPath(guid));
                    if(building.StatsMigrated)continue;
                    building.MigrateStats();Save(building);
                }
                foreach(string guid in AssetDatabase.FindAssets("t:ProgressionCatalog",new[]{"Assets"}))
                    MigrateProgression(AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(AssetDatabase.GUIDToAssetPath(guid)));
            }
            catch(Exception exception) { Debug.LogError("SO 데이터 이전 중단 (기존 값을 덮어쓰지 않습니다): "+exception); }
        }
        private static void MigrateSettings(PrototypeSettings settings,string path)
        {
            string folder=Path.GetDirectoryName(path).Replace('\\','/')+"/"+Path.GetFileNameWithoutExtension(path)+"Data";
            EnsureFolder(folder);
            if(settings.Camera==null)
            {
                var child=Create<PrototypeCameraSettings>(folder+"/Camera.asset", data=>
                {
                    data.CameraGroundDistance=settings.CameraGroundDistance;
                    data.CameraEdgeBand=settings.CameraEdgeBand;
                    data.CameraFastBand=settings.CameraFastBand;
                    data.CameraMediumBand=settings.CameraMediumBand;
                    data.CameraSlowSpeed=settings.CameraSlowSpeed;
                    data.CameraMediumSpeed=settings.CameraMediumSpeed;
                    data.CameraMaxSpeed=settings.CameraMaxSpeed;
                });
                settings.Camera=child;
            }
            if(settings.Presentation==null)
            {
                var child=Create<PrototypePresentationSettings>(folder+"/Presentation.asset", data=>
                {
                    data.VisionBlur=settings.VisionBlur;
                    data.PixelHeight=settings.PixelHeight;
                    data.TreeModelScale=settings.TreeModelScale;
                    data.RockModelScale=settings.RockModelScale;
                    data.GrassSizeScale=settings.GrassSizeScale;
                    data.GrassUsesTerrainColor=settings.GrassUsesTerrainColor;
                    data.GrassBaseColor=settings.GrassBaseColor;
                    data.GrassBeigeColor=settings.GrassBeigeColor;
                    data.GrassDarkColor=settings.GrassDarkColor;
                    data.GrassPaleColor=settings.GrassPaleColor;
                    data.GrassTealColor=settings.GrassTealColor;
                    data.GrassMossColor=settings.GrassMossColor;
                    data.GrassPurpleColor=settings.GrassPurpleColor;
                    data.GrassFlowerPink=settings.GrassFlowerPink;
                    data.GrassFlowerYellow=settings.GrassFlowerYellow;
                });
                settings.Presentation=child;
            }
            if(settings.Audio==null)
            {
                var child=Create<PrototypeAudioSettings>(folder+"/Audio.asset", data=>
                {
                    data.SoundVolume=settings.SoundVolume;
                    foreach(var cue in data.Cues)cue.Clips=cue.ResourcePaths.Select(Resources.Load<AudioClip>).Where(clip=>clip!=null).ToArray();
                });
                settings.Audio=child;
            }
            if(settings.World==null)
            {
                var child=Create<PrototypeWorldSettings>(folder+"/World.asset", data=>
                {
                    data.MapSeed=settings.MapSeed;
                    data.MapRadius=settings.MapRadius;
                    data.ResourceSpawnSeconds=settings.ResourceSpawnSeconds;
                    data.TreeLimit=settings.TreeLimit;
                    data.RockLimit=settings.RockLimit;
                });
                settings.World=child;
            }
            if(settings.System==null)
            {
                var child=Create<PrototypeSystemSettings>(folder+"/System.asset", data=>
                {
                    data.FirstPreparation=settings.FirstPreparation;
                    data.Maintenance=settings.Maintenance;
                    data.CursorRadius=settings.CursorRadius;
                    data.HouseRadius=settings.HouseRadius;
                    data.WoodPerSecond=settings.WoodPerSecond;
                    data.StonePerSecond=settings.StonePerSecond;
                    data.CursorDamage=settings.CursorDamage;
                    data.RepairPerSecond=settings.RepairPerSecond;
                    data.WarriorSeconds=settings.WarriorSeconds;
                    data.WarriorMoney=settings.WarriorMoney;
                    data.DaySeconds=settings.DaySeconds;
                    data.DuskSeconds=settings.DuskSeconds;
                    data.NightSeconds=settings.NightSeconds;
                    data.ContributionSeconds=settings.ContributionSeconds;
                    data.ContributionPower=settings.ContributionPower;
                    data.DivinePowerMaximum=settings.DivinePowerMaximum;
                    data.DivineFieldCost=settings.DivineFieldCost;
                    data.DivinePowerRegeneration=settings.DivinePowerRegeneration;
                    data.DivineFieldDuration=settings.DivineFieldDuration;
                    data.CursorVision=settings.CursorVision;
                    data.VillageVision=settings.VillageVision;
                    data.VisionTrailSeconds=settings.VisionTrailSeconds;
                    data.AutoConstruction=settings.AutoConstruction;
                    data.PrototypeDays=settings.PrototypeDays;
                });
                settings.System=child;
            }
            if(settings.Units==null)settings.Units=Create<PrototypeUnitData>(folder+"/Units.asset", data=>data.Values.Find(Kind.Warrior).Price=settings.WarriorMoney);
            if(settings.Waves==null)settings.Waves=Create<PrototypeWaveData>(folder+"/Waves.asset");
            if(settings.Resources==null)settings.Resources=Create<PrototypeResourceData>(folder+"/Resources.asset");
            if(settings.Economy==null)settings.Economy=Create<PrototypeEconomyData>(folder+"/Economy.asset");
            if(settings.Combat==null)settings.Combat=Create<PrototypeCombatData>(folder+"/Combat.asset");
            if(settings.Growth==null)settings.Growth=Create<PrototypeGrowthData>(folder+"/Growth.asset");
            if(settings.Rebirth==null)settings.Rebirth=Create<PrototypeRebirthData>(folder+"/Rebirth.asset");
            if(settings.Relics==null)settings.Relics=Create<PrototypeRelicEffectsData>(folder+"/Relics.asset");
            if(settings.Loot==null)settings.Loot=Create<PrototypeLootData>(folder+"/Loot.asset");
            if(settings.Chests==null)settings.Chests=Create<PrototypeChestData>(folder+"/Chests.asset");
            if(settings.Environment==null)settings.Environment=Create<PrototypeEnvironmentSettings>(folder+"/Environment.asset");
            if(settings.Destruction==null)settings.Destruction=Create<Afterlife.Modules.Destruction.DestructionProfile>(folder+"/Destruction.asset");
            if(settings.WorldVisuals==null)settings.WorldVisuals=Create<PrototypeWorldCatalog>(folder+"/WorldVisuals.asset",data=>
                data.Entries=Resources.LoadAll<GameObject>("AfterlifePrototype/World").OrderBy(asset=>asset.name).Select(asset=>new PrototypeWorldAsset{Id=asset.name,Prefab=asset}).ToList());
            if(settings.Audio.WorldBank==null)settings.Audio.WorldBank=Resources.Load<Afterlife.Modules.WorldAudio.WorldAudioBank>("WorldAudio/WorldAudioBank");
            if(settings.Audio.GrassRustles==null||settings.Audio.GrassRustles.Length==0)settings.Audio.GrassRustles=new[]{Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_02"),Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_03"),Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_04")};
            if(settings.Audio.Bird==null)settings.Audio.Bird=Resources.Load<AudioClip>("AmbientSounds/bird");
            // 나무/돌 큐는 기존 WorldAudioBank가 관리하므로 중복 편집 항목을 제거한다.
            settings.Audio.Cues.RemoveAll(cue=>cue.Id==SoundCue.Wood);Save(settings.Audio);
            settings.ValidateData();settings.RulesSnapshot().Validate();
            foreach(var field in typeof(PrototypeSettings).GetFields())if(field.GetValue(settings) is ScriptableObject child)Save(child);
            settings.MarkMigrated();Save(settings);
            Debug.Log("SO 데이터 분리 완료: "+path,settings);
        }
        private static void MigrateProgression(ProgressionCatalog catalog)
        {
            if(catalog.TreeAssets.Count>0&&catalog.SkillCatalog!=null)return;
            string path=AssetDatabase.GetAssetPath(catalog);
            string folder=Path.GetDirectoryName(path).Replace('\\','/')+"/"+Path.GetFileNameWithoutExtension(path)+"Data";
            EnsureFolder(folder);
            var trees=catalog.Trees.ToArray();var skills=catalog.Skills.ToArray();
            if(catalog.TreeAssets.Count==0)
                for(int i=0;i<trees.Length;i++)
                {
                    var tree=trees[i];string name=tree.Id=="research"?"Research":tree.Id=="altar"?"Altar":"Tree"+i;
                    catalog.TreeAssets.Add(Create<UpgradeTreeAsset>(folder+"/"+name+".asset",asset=>asset.Data=JsonUtility.FromJson<UpgradeTreeData>(JsonUtility.ToJson(tree))));
                }
            if(catalog.SkillCatalog==null)catalog.SkillCatalog=Create<SkillCatalog>(folder+"/Skills.asset",asset=>asset.Entries=skills.Select(skill=>JsonUtility.FromJson<SkillData>(JsonUtility.ToJson(skill))).ToList());
            var errors=catalog.Validate();if(errors.Count>0)throw new InvalidOperationException(string.Join(" / ",errors));
            Save(catalog);
        }
        private static T Create<T>(string path,Action<T> initialize=null) where T:ScriptableObject
        {
            var existing=AssetDatabase.LoadAssetAtPath<T>(path);
            if(existing!=null)return existing;
            if(File.Exists(path))throw new IOException("기존 에셋을 읽을 수 없습니다: "+path);
            var asset=ScriptableObject.CreateInstance<T>();
            initialize?.Invoke(asset);AssetDatabase.CreateAsset(asset,path);Save(asset);return asset;
        }
        private static void Save(UnityEngine.Object asset) { EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset); }
        private static void EnsureFolder(string path)
        {
            if(AssetDatabase.IsValidFolder(path))return;
            string parent=Path.GetDirectoryName(path).Replace('\\','/');EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent,Path.GetFileName(path));
        }
    }
}
