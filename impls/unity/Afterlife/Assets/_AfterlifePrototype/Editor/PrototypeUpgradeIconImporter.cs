using Afterlife.Modules.Progression;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Prototype.Editor
{
    // 이 폴더에는 기술/시각 검수 후 승인된 저해상도 PNG만 들어온다.
    public sealed class PrototypeUpgradeIconImporter : AssetPostprocessor
    {
        private const string Folder = "Assets/_AfterlifePrototype/Resources/UI/Upgrades/";
        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaIsTransparency = true; importer.wrapMode = TextureWrapMode.Clamp; importer.spritePixelsPerUnit = 48;
            importer.maxTextureSize = 64;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings);
        }
        [InitializeOnLoadMethod]
        private static void Initialize() { EditorApplication.delayCall += BindMissingIcons; }
        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (string path in imported)
                if (path.StartsWith(Folder) || path == PrototypeProgressionEditor.CatalogPath)
                { EditorApplication.delayCall -= BindMissingIcons; EditorApplication.delayCall += BindMissingIcons; break; }
        }
        private static void BindMissingIcons()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            var catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(PrototypeProgressionEditor.CatalogPath);
            if (catalog == null) return;
            bool changed = false;
            foreach (var node in catalog.Nodes)
            {
                if (node.Icon != null) continue; // 사용자 Sprite 지정은 보존한다.
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + node.Id + ".png");
                if (sprite == null) continue; node.Icon = sprite; changed = true;
            }
            if (changed) { foreach(var asset in catalog.DataAssets){EditorUtility.SetDirty(asset);AssetDatabase.SaveAssetIfDirty(asset);} }
        }
    }
}
