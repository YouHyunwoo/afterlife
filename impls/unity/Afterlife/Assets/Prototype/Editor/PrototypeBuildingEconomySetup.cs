using System;
using System.Linq;
using Afterlife.Modules.Progression;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Prototype.Editor
{
    /// <summary>Explicit, repeatable authoring migration. Never runs on import or during play.</summary>
    public static class PrototypeBuildingEconomySetup
    {
        private const string DataRoot = "Assets/Prototype/Resources/Data/";
        private const string SettingsRoot = "Assets/Prototype/Resources/AfterlifePrototype/";

        public static string Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("플레이를 종료한 뒤 건물 데이터를 이전하세요.");
            var catalog = AssetDatabase.LoadAssetAtPath<PrototypeBuildCatalog>(DataRoot + "BuildCatalog.asset");
            var visuals = AssetDatabase.LoadAssetAtPath<PrototypeWorldCatalog>(SettingsRoot + "SettingsData/WorldVisuals.asset");
            if (catalog == null || visuals == null) throw new InvalidOperationException("본편 건물/표현 카탈로그가 없습니다.");
            Undo.RecordObject(catalog, "Configure automatic building economy");
            foreach (var asset in catalog.Buildings.Where(a => a != null))
            {
                Undo.RecordObject(asset, "Migrate building rules");
                bool firstMigration = asset.BuildingRulesVersion < 1;
                asset.MigrateBuildingRules();
                if (firstMigration && asset.Id == Kind.Barracks) asset.GoldCost = 80;
                EditorUtility.SetDirty(asset);
            }
            AddBuilding(catalog, Kind.TownHall, Kind.House, 0);
            AddBuilding(catalog, Kind.Sanctuary, Kind.Altar, 0);
            AddBuilding(catalog, Kind.ArcheryRange, Kind.Barracks, 120);
            AddBuilding(catalog, Kind.KnightHall, Kind.Barracks, 320);
            AddBuilding(catalog, Kind.MageTower, Kind.Altar, 240);
            // Keep merged definitions in the catalog so the model receives their SO snapshots.
            catalog.Buildings.RemoveAll(a => a == null || a.Id == Kind.Research || a.Id == Kind.Market);
            foreach (var asset in catalog.Buildings)
            {
                if (asset.Id == Kind.House) asset.DisplayName = "주택";
                if (asset.Id == Kind.Farm) asset.DisplayName = "농장";
                EditorUtility.SetDirty(asset);
            }
            EditorUtility.SetDirty(catalog);
            AddVisual(visuals, Kind.TownHall, Kind.House, new Vector3(1.8f, 1.25f, 1.8f));
            AddVisual(visuals, Kind.Sanctuary, Kind.Altar, new Vector3(1.8f, 1.4f, 1.8f));
            AddVisual(visuals, Kind.ArcheryRange, Kind.Barracks, new Vector3(1, .8f, 1));
            AddVisual(visuals, Kind.KnightHall, Kind.Barracks, new Vector3(1, 1.35f, 1));
            AddVisual(visuals, Kind.MageTower, Kind.MagicTower, new Vector3(.85f, 1.2f, .85f));
            foreach (var name in new[] { "MvpResearch", "Research", "Altar" })
                MigrateTree(AssetDatabase.LoadAssetAtPath<UpgradeTreeAsset>(SettingsRoot + "ProgressionData/" + name + ".asset"));
            var world = AssetDatabase.LoadAssetAtPath<PrototypeWorldSettings>(SettingsRoot + "SettingsData/World.asset");
            if (world != null) EditorUtility.SetDirty(world);
            var units = AssetDatabase.LoadAssetAtPath<PrototypeUnitData>(SettingsRoot + "SettingsData/Units.asset");
            if (units != null)
            {
                foreach (var unit in units.Values.Entries)
                    if (unit.Id == Kind.Knight && unit.RequiredBuilding == Kind.Research) unit.RequiredBuilding = Kind.Altar;
                EditorUtility.SetDirty(units);
            }
            ConfigureProductionPanel();
            ConfigureUpgradeCurrencyIcon();
            AssetDatabase.SaveAssets();
            return "건물 SO, 합체/병종 표현 프리팹, 에테론 강화 비용을 저장했습니다.";
        }

        private static void AddBuilding(PrototypeBuildCatalog catalog, Kind kind, Kind sourceKind, int cost)
        {
            string path = DataRoot + "Buildings/" + kind + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<PrototypeBuildingData>(path);
            if (asset == null)
            {
                var source = catalog.Buildings.First(a => a != null && a.Id == sourceKind);
                asset = ScriptableObject.CreateInstance<PrototypeBuildingData>();
                asset.Id = kind;
                asset.DisplayName = VillageModel.Name(kind);
                asset.Icon = source.Icon;
                asset.GoldCost = cost;
                asset.MigrateStats();
                asset.MigrateBuildingRules();
                asset.RequiresBuilding = kind == Kind.KnightHall || kind == Kind.MageTower;
                asset.RequiredBuilding = Kind.Altar;
                AssetDatabase.CreateAsset(asset, path);
            }
            if (!catalog.Buildings.Contains(asset)) catalog.Buildings.Add(asset);
        }

        private static void AddVisual(PrototypeWorldCatalog catalog, Kind kind, Kind sourceKind, Vector3 scale)
        {
            if (catalog.Find(kind.ToString()) != null) return;
            var source = catalog.Find(sourceKind.ToString());
            if (source == null) throw new InvalidOperationException("표현 원본 누락: " + sourceKind);
            const string folder = "Assets/Prototype/Prefabs/BuildingEconomy";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets/Prototype/Prefabs", "BuildingEconomy");
            string path = folder + "/" + kind + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var root = new GameObject(kind.ToString());
                try
                {
                    var art = (GameObject)PrefabUtility.InstantiatePrefab(source, root.transform);
                    art.name = "Art";
                    art.transform.localScale = scale;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            Undo.RecordObject(catalog, "Add building visual");
            catalog.Entries.Add(new PrototypeWorldAsset { Id = kind.ToString(), Prefab = prefab });
            EditorUtility.SetDirty(catalog);
        }

        private static void MigrateTree(UpgradeTreeAsset tree)
        {
            if (tree == null) return;
            Undo.RecordObject(tree, "Convert upgrades to Etheron");
            tree.Data.Title = "제단 · 마을 / 마법";
            tree.Data.Laboratory = false;
            foreach (var node in tree.Data.Nodes)
            {
                if (!tree.EtheronCostsMigrated)
                    foreach (var level in node.Levels) level.Cost = (int)Math.Ceiling(level.Cost / 30d);
                node.Description = node.Description.Replace("연구소", "제단").Replace("골드", "에테론");
                if (node.EffectId == "housing" || node.EffectId == "territory")
                {
                    node.EffectId = "radius"; node.Title = "영력 범위 확장";
                    node.Description = "주민 수와 무관하게 작업 범위를 늘립니다.";
                    for (int i = 0; i < node.Levels.Count; i++) node.Levels[i].Value = .1f * (i + 1);
                    node.ValueFormat = "P1"; node.ValueSuffix = "";
                }
                else if (node.EffectId == "education")
                {
                    node.EffectId = "work"; node.Title = "작업 숙련";
                    node.Description = "채집·건설·수리 작업 효율을 직접 강화합니다.";
                    node.ValueFormat = "P1"; node.ValueSuffix = "";
                }
            }
            tree.EtheronCostsMigrated = true;
            EditorUtility.SetDirty(tree);
        }

        private static void ConfigureProductionPanel()
        {
            const string path = "Assets/Prototype/Prefabs/Modal/Barracks.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
                {
                    if (text.name == "Title Text") text.text = "자동 생산";
                    if (text.name == "Barracks.Status") text.text = "완공 후 자동으로 생산합니다.";
                    if (text.name == "Barracks.Queue")
                    { text.text = "생산 효과"; text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 82); }
                    if (text.name == "Queue Help")
                    { text.text = "가속은 현재 생산 회차에만 적용됩니다."; text.rectTransform.anchoredPosition = new Vector2(0, -190); }
                }
                foreach (var rect in root.GetComponentsInChildren<RectTransform>(true))
                    if (rect.name.StartsWith("Troop ") || rect.name.StartsWith("Barracks.Slot.")) rect.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void ConfigureUpgradeCurrencyIcon()
        {
            const string path = "Assets/Prototype/Prefabs/UpgradeTreeScreen.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var tooltip = root.GetComponentInChildren<UpgradeHoverTooltip>(true);
                if (tooltip != null)
                {
                    var serialized = new SerializedObject(tooltip);
                    var coin = serialized.FindProperty("_coin").objectReferenceValue as RectTransform;
                    if (coin != null)
                        foreach (var graphic in coin.GetComponentsInChildren<UnityEngine.UI.Graphic>(true))
                            graphic.color = new Color(.55f, .82f, 1f, graphic.color.a);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static string ConnectHud()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("플레이 중에는 연결할 수 없습니다.");
            var ui = UnityEngine.Object.FindObjectsByType<PrototypeCanvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .FirstOrDefault(c => c.gameObject.scene.name == PrototypeScene.SceneName);
            if (ui == null) throw new InvalidOperationException("본편 씬을 열어 주세요.");
            if (ui.Bindings.Any(b => b != null && b.Id == "HUD.Etheron"))
            {
                FitResourceHud(ui);
                UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ui.gameObject.scene);
                return "Etheron HUD already connected; layout refreshed.";
            }
            if (ui.gameObject.scene.isDirty) throw new InvalidOperationException("기존 씬 편집을 먼저 저장해야 합니다.");
            var money = ui.Bindings.First(b => b != null && b.Id == "HUD.Money");
            if (money.TmpText == null || money.Rect == null) throw new InvalidOperationException("기존 골드 TMP 참조가 없습니다.");
            const string path = "Assets/Prototype/Prefabs/BuildingEconomy/EtheronHud.prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                var root = new GameObject("EtheronHud", typeof(RectTransform), typeof(UnityEngine.UI.LayoutElement), typeof(UnityEngine.UI.HorizontalLayoutGroup));
                try
                {
                    ((RectTransform)root.transform).sizeDelta = new Vector2(300, 32);
                    var size = root.GetComponent<UnityEngine.UI.LayoutElement>(); size.minHeight = size.preferredHeight = 32;
                    var group = root.GetComponent<UnityEngine.UI.HorizontalLayoutGroup>();
                    group.spacing = 8; group.childControlWidth = true; group.childControlHeight = true;
                    group.childForceExpandWidth = false; group.childForceExpandHeight = false;
                    var text = new GameObject("EtheronLabel", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI), typeof(UnityEngine.UI.LayoutElement));
                    text.transform.SetParent(root.transform, false);
                    var label = text.GetComponent<TMPro.TextMeshProUGUI>();
                    label.font = money.TmpText.font; label.fontSize = 17; label.color = new Color(.65f,.85f,1);
                    label.text = "에테론 0"; label.raycastTarget = false;
                    var textSize = text.GetComponent<UnityEngine.UI.LayoutElement>(); textSize.preferredWidth = 180; textSize.preferredHeight = 32;
                    var buttonObject = new GameObject("AltarButton", typeof(RectTransform), typeof(UnityEngine.UI.Image), typeof(UnityEngine.UI.Button), typeof(UnityEngine.UI.LayoutElement));
                    buttonObject.transform.SetParent(root.transform, false);
                    var buttonSize = buttonObject.GetComponent<UnityEngine.UI.LayoutElement>(); buttonSize.preferredWidth = 104; buttonSize.preferredHeight = 30;
                    var background = buttonObject.GetComponent<UnityEngine.UI.Image>(); background.color = new Color(.13f,.2f,.29f,.95f);
                    var button = buttonObject.GetComponent<UnityEngine.UI.Button>(); button.targetGraphic = background; button.interactable = false;
                    var captionObject = new GameObject("Caption", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                    captionObject.transform.SetParent(buttonObject.transform, false);
                    var captionRect = (RectTransform)captionObject.transform; captionRect.anchorMin = Vector2.zero; captionRect.anchorMax = Vector2.one; captionRect.offsetMin = captionRect.offsetMax = Vector2.zero;
                    var caption = captionObject.GetComponent<TMPro.TextMeshProUGUI>(); caption.font = money.TmpText.font; caption.fontSize = 14;
                    caption.alignment = TMPro.TextAlignmentOptions.Center; caption.text = "제단 · 잠김"; caption.raycastTarget = false;
                    prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, money.Rect.parent.parent);
            Undo.RegisterCreatedObjectUndo(instance, "Install Etheron HUD");
            instance.transform.SetSiblingIndex(money.Rect.parent.GetSiblingIndex() + 1);
            var etheron = instance.transform.Find("EtheronLabel").GetComponent<TMPro.TMP_Text>();
            var altar = instance.transform.Find("AltarButton").GetComponent<UnityEngine.UI.Button>();
            Undo.RecordObject(ui, "Connect Etheron HUD bindings");
            ui.Bindings.Add(new PrototypeUiBinding { Id = "HUD.Etheron", Rect = etheron.rectTransform, TmpText = etheron });
            ui.Bindings.Add(new PrototypeUiBinding { Id = "HUD.Altar", Rect = (RectTransform)altar.transform, Button = altar, TmpText = altar.GetComponentInChildren<TMPro.TMP_Text>() });
            ui.CameraGuards.Add((RectTransform)instance.transform);
            FitResourceHud(ui);
            EditorUtility.SetDirty(ui);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ui.gameObject.scene)) throw new InvalidOperationException("HUD 씬 저장 실패");
            return "돈 아래 에테론 표시와 제단 버튼 프리팹을 연결했습니다.";
        }

        private static void FitResourceHud(PrototypeCanvas ui)
        {
            var money = ui.Bindings.First(b => b != null && b.Id == "HUD.Money");
            var group = money.Rect.parent.parent as RectTransform;
            var resources = group.parent as RectTransform;
            Undo.RecordObject(resources, "Fit resource HUD");
            resources.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 300);
            resources.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 112);
            var layout = group.GetComponent<UnityEngine.UI.VerticalLayoutGroup>();
            if (layout != null)
            {
                Undo.RecordObject(layout, "Layout resource HUD rows");
                layout.enabled = true;
            }
            UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(group);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ui.gameObject.scene);
        }
    }
}
