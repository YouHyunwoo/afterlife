using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Modules.Progression;
using Afterlife.Modules.RechargeSlots.Unity;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Prototype.Editor
{
    // Explicitly invoked authoring operation; no auto migration and no new Tools menu.
    public static class PrototypeMvpSetup
    {
        private const string Root = "Assets/_AfterlifePrototype/Resources/AfterlifePrototype/";
        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이를 종료한 뒤 적용하세요.");
            var settings = AssetDatabase.LoadAssetAtPath<PrototypeSettings>(Root + "Settings.asset");
            if (settings == null || settings.System == null) throw new InvalidOperationException("본편 설정이 없습니다.");
            CreateProgression();
            CreateHud();
            AddRelicPresentation();
            // Only this settings assembly opts in. Other consumers keep their energy mode.
            Set(settings.System, "MvpSlots", true);
            Set(settings.System, "CursorRadius", .5f);
            Set(settings.System, "InitialSlots", 1);
            Set(settings.System, "MaximumSlots", 3);
            Set(settings.System, "SlotRechargeSeconds", 10f);
            Set(settings.System, "FirstDaySeconds", 90f);
            Set(settings.System, "SecondDaySeconds", 60f);
            Set(settings.System, "DaySeconds", 45f);
            Set(settings.System, "DuskSeconds", 10f);
            Set(settings.System, "NightSeconds", 35f);
            Set(settings.System, "PrototypeDays", 6);
            Set(settings.Economy, "Values.RadiusPerResident", .08f);
            Set(settings.Relics, "Values.ResourceCapacity", 1f);
            Set(settings.Relics, "Values.RegrowthMultiplier", .5f);
            AssetDatabase.SaveAssets();
            return "MVP settings, 22-node research, three rewards and FieldSlotsHud saved.";
        }
        public static string ConnectSceneHud()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 중에는 설치할 수 없습니다.");
            var view = UnityEngine.Object.FindObjectsByType<PrototypeView>(FindObjectsInactive.Include,FindObjectsSortMode.None)
                .FirstOrDefault(v=>v.gameObject.scene.name==PrototypeScene.SceneName);
            if (view==null) throw new InvalidOperationException("본편 씬을 연 뒤 실행하세요.");
            if (view.gameObject.scene.isDirty) throw new InvalidOperationException("기존 씬 편집을 먼저 저장한 뒤 실행하세요.");
            var serialized = new SerializedObject(view);
            var property = serialized.FindProperty("_fieldSlotsHud");
            if (property.objectReferenceValue != null) return "HUD already connected.";
            var label = serialized.FindProperty("_divinePowerLabel").objectReferenceValue as TMPro.TMP_Text;
            if (label==null) throw new InvalidOperationException("기존 영력 HUD 참조가 없습니다.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root+"FieldSlotsHud.prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab,label.transform.parent);
            Undo.RegisterCreatedObjectUndo(instance,"Install field slots HUD");
            var rect=(RectTransform)instance.transform;
            rect.anchorMin=label.rectTransform.anchorMin;rect.anchorMax=label.rectTransform.anchorMax;rect.pivot=label.rectTransform.pivot;
            rect.anchoredPosition=label.rectTransform.anchoredPosition;rect.sizeDelta=new Vector2(164,6);
            PrefabUtility.RecordPrefabInstancePropertyModifications(rect);
            property.objectReferenceValue=instance.GetComponent<RechargeSlotsHud>();serialized.ApplyModifiedProperties();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
            if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(view.gameObject.scene))throw new InvalidOperationException("씬 저장에 실패했습니다.");
            return "FieldSlotsHud prefab instance and adapter reference saved in prototype scene.";
        }
        private static void Set(UnityEngine.Object target, string path, object value)
        {
            Undo.RecordObject(target, "Configure six-day MVP");
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(path) ?? throw new InvalidOperationException(path);
            if (value is bool flag) property.boolValue = flag;
            else if (value is int integer) property.intValue = integer;
            else property.floatValue = (float)value;
            serialized.ApplyModifiedProperties();
        }
        private static void CreateProgression()
        {
            if (AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(Root + "MvpProgression.asset") != null) return;
            var source = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(Root + "Progression.asset");
            if (source == null) throw new InvalidOperationException("기존 강화 카탈로그가 없습니다.");
            var tree = ScriptableObject.CreateInstance<UpgradeTreeAsset>();
            tree.Data = new UpgradeTreeData { Id="research", Title="연구소 · 마을 / 마법", Laboratory=true };
            foreach (var original in source.Trees)
            foreach (var node in original.Nodes)
            {
                var copy = JsonUtility.FromJson<UpgradeNodeData>(JsonUtility.ToJson(node));
                // Preserve object references explicitly across serialization roundtrip.
                copy.Icon = node.Icon;
                copy.RequiresAltar = original.Id == "altar";
                copy.Grid += new Vector2Int(original.Id == "altar" ? 6 : -6, 0);
                if (copy.RequiresAltar) copy.Description = "제단 필요 · " + copy.Description.Replace("[Q]", "자동:").Replace("[W]", "자동:").Replace("[E]", "자동:").Replace("커서 안", "마을 영역 안").Replace("커서 범위", "마을 영역");
                if (copy.Id == "R8") copy.Title = "필드 작업 속도";
                tree.Data.Nodes.Add(copy);
            }
            var work = tree.Data.Nodes.Find(n=>n.Id=="R2");
            var speed = tree.Data.Nodes.Find(n=>n.Id=="R8");
            tree.Data.Nodes.Add(new UpgradeNodeData { Id="R11",Title="추가 필드 슬롯",Description="필드 충전 슬롯 +1개. 설치 즉시 각 슬롯이 따로 충전됩니다. 일일 보상과 합쳐 최대 3개.",EffectId="fieldSlot",Grid=new Vector2Int(-3,-2),Icon=work.Icon,ValueSuffix="개",Levels=new List<UpgradeLevelData>{new UpgradeLevelData{Cost=100,Value=1}} });
            tree.Data.Nodes.Add(new UpgradeNodeData { Id="R12",Title="슬롯 재충전",Description="슬롯 충전 시간을 줄입니다. 내부 작업 속도와 별개이며 일일 보상과 합산 최대 50% 감소.",EffectId="slotRecharge",Grid=new Vector2Int(-6,-2),Icon=speed.Icon,Parents=new List<string>{"R2"},ValueFormat="P1",Levels=new List<UpgradeLevelData>{new UpgradeLevelData{Cost=60,Value=.1f},new UpgradeLevelData{Cost=120,Value=.2f},new UpgradeLevelData{Cost=180,Value=.3f}} });
            var catalog = ScriptableObject.CreateInstance<ProgressionCatalog>();
            catalog.TreeAssets.Add(tree); catalog.SkillCatalog = source.SkillCatalog;
            if (catalog.SkillCatalog == null) catalog.Skills = source.Skills;
            var errors = PrototypeProgressionDefaults.Validate(catalog, true);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join(" / ",errors));
            AssetDatabase.CreateAsset(tree, Root + "ProgressionData/MvpResearch.asset");
            AssetDatabase.CreateAsset(catalog, Root + "MvpProgression.asset");
        }
        private static void AddRelicPresentation()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeRelicCatalog>("Assets/_AfterlifePrototype/Resources/Data/RelicCatalog.asset");
            if (catalog == null) throw new InvalidOperationException("유물 표시 카탈로그가 없습니다.");
            Undo.RecordObject(catalog,"Add MVP reward presentation");
            var tree = AssetDatabase.LoadAssetAtPath<UpgradeTreeAsset>(Root+"ProgressionData/MvpResearch.asset");
            foreach (var pair in new[]{(RelicKind.FieldRadius,"R3"),(RelicKind.SlotRecharge,"R12"),(RelicKind.FieldSlot,"R11")})
                if (catalog.Find(pair.Item1)==null) catalog.Entries.Add(new PrototypeRelicData{Id=pair.Item1,DisplayName=VillageModel.RelicName(pair.Item1),Icon=tree.Data.Nodes.Find(n=>n.Id==pair.Item2).Icon});
            EditorUtility.SetDirty(catalog);
        }
        private static void CreateHud()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root+"FieldSlotsHud.prefab") != null) return;
            var root = new GameObject("FieldSlotsHud", typeof(RectTransform), typeof(RechargeSlotsHud));
            try
            {
                var rect = (RectTransform)root.transform; rect.sizeDelta = new Vector2(164,6);
                for(int i=0;i<3;i++)
                {
                    var background = new GameObject("Slot"+i, typeof(RectTransform),typeof(UnityEngine.UI.Image));
                    var r = (RectTransform)background.transform; r.SetParent(rect,false);r.anchorMin=r.anchorMax=new Vector2(0,.5f);r.pivot=new Vector2(0,.5f);r.anchoredPosition=new Vector2(i*58,0);r.sizeDelta=new Vector2(48,6);
                    var image=background.GetComponent<UnityEngine.UI.Image>();image.color=new Color(1,1,1,.15f);image.raycastTarget=false;
                    var fill = new GameObject("Fill",typeof(RectTransform),typeof(UnityEngine.UI.Image));
                    var f=(RectTransform)fill.transform;f.SetParent(r,false);f.anchorMin=Vector2.zero;f.anchorMax=Vector2.one;f.offsetMin=f.offsetMax=Vector2.zero;
                    var foreground=fill.GetComponent<UnityEngine.UI.Image>();foreground.color=Color.white;foreground.raycastTarget=false;
                }
                PrefabUtility.SaveAsPrefabAsset(root,Root+"FieldSlotsHud.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
    }
}
