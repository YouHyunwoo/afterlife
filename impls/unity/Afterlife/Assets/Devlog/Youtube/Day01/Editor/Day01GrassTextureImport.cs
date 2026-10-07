using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    [InitializeOnLoad]
    public sealed class Day01GrassTextureImport : AssetPostprocessor
    {
        private static string Folder => AssetDatabase.IsValidFolder("Assets/Modules/Vegetation/Resources/GrassSprites")
            ? "Assets/Modules/Vegetation/Resources/GrassSprites/" : "Assets/Devlog/Youtube/Day01/Resources/GrassSprites/";
        static Day01GrassTextureImport() { EditorApplication.delayCall+=ConfigureExisting; }
        private static void Configure(TextureImporter importer)
        {
            importer.textureType=TextureImporterType.Default;
            importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency=true; importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point; importer.wrapMode=TextureWrapMode.Clamp;
            importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.npotScale=TextureImporterNPOTScale.None;
        }
        private void OnPreprocessTexture()
        {
            if ((assetPath.StartsWith("Assets/Devlog/") || assetPath.StartsWith("Assets/Modules/Vegetation/"))
                && assetPath.Contains("/Resources/GrassSprites/")) Configure((TextureImporter)assetImporter);
        }
        private static void ConfigureExisting()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            { EditorApplication.delayCall+=ConfigureExisting; return; }
            for (int i=0;i<4;i++)
            {
                var importer=AssetImporter.GetAtPath(Folder+"Grass"+i+".png") as TextureImporter;
                if (importer==null) continue;
                if (importer.filterMode==FilterMode.Point && !importer.mipmapEnabled && importer.alphaIsTransparency
                    && importer.textureCompression==TextureImporterCompression.Uncompressed && importer.wrapMode==TextureWrapMode.Clamp) continue;
                Configure(importer); importer.SaveAndReimport();
            }
        }
    }
}
