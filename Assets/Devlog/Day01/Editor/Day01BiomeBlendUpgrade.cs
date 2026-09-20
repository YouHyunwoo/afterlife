using Afterlife.Devlog.Terrain;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Devlog.Day01.Editor
{
    public static class Day01BiomeBlendUpgrade
    {
        public static void ApplyTo(Day01Showcase showcase)
        {
            if (showcase.LayoutVersion>=11 || showcase.Stages.Length<3) return;
            const string root=Day01SceneBuilder.Root;
            var palette=new Color[7];
            for (int i=0;i<palette.Length;i++)
            {
                var source=AssetDatabase.LoadAssetAtPath<Material>(root+"/Materials/TerrainVariant_"+(ShowcaseBiome)i+".mat");
                palette[i]=source!=null ? source.GetColor("_BaseColor") : Color.white;
            }
            const string texturePath=root+"/Materials/AfterBiomeBlend.asset";
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture==null)
            {
                texture=new Texture2D(384,320,TextureFormat.RGBA32,false,false) { name="AfterBiomeBlend" };
                AssetDatabase.CreateAsset(texture,texturePath);
            }
            Undo.RecordObject(texture,"육지 경계 색상 생성");
            texture.filterMode=FilterMode.Point; texture.wrapMode=TextureWrapMode.Clamp;
            var pixels=new Color[texture.width*texture.height]; var weights=new double[7];
            for (int z=0;z<texture.height;z++)
            for (int x=0;x<texture.width;x++)
            {
                ShowcaseBiomeBlend.Weights(-6+(x+.5)/32,-5+(z+.5)/32,weights);
                Color color=Color.clear;
                for (int i=0;i<palette.Length;i++) color+=palette[i]*(float)weights[i];
                color.a=1; pixels[z*texture.width+x]=color;
            }
            texture.SetPixels(pixels); texture.Apply(false,false); EditorUtility.SetDirty(texture);
            const string materialPath=root+"/Materials/AfterBlendedTerrain.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material==null)
            {
                material=new Material(Shader.Find("Afterlife/Devlog/ShowcaseSurface")) { name="AfterBlendedTerrain" };
                AssetDatabase.CreateAsset(material,materialPath);
            }
            Undo.RecordObject(material,"완성 단계 육지 경계 혼합");
            var stage=showcase.Stages[2];
            material.SetColor("_BaseColor",Color.white); material.SetFloat("_TerrainBlend",1);
            material.SetFloat("_Grid",0); material.SetFloat("_Stylized",.25f);
            material.SetTexture("_TerrainColorMap",texture);
            material.SetVector("_TerrainMapBounds",new Vector4(stage.position.x-6,stage.position.z-5,12,10));
            EditorUtility.SetDirty(material);
            var grid=stage.Find("Terrain_Flat/BiomeGrid_12x10");
            foreach (Transform tile in grid)
            {
                if (!tile.name.StartsWith("Tile_") || tile.name.EndsWith("_Water") || !tile.gameObject.activeSelf) continue;
                var renderer=tile.GetComponent<MeshRenderer>();
                if (renderer==null) continue;
                var materials=renderer.sharedMaterials;
                if (materials.Length<2) continue;
                Undo.RecordObject(renderer,"육지 윗면 혼합 재질 적용");
                materials[0]=material; renderer.sharedMaterials=materials;
            }
            Undo.RecordObject(showcase,"육지 경계 버전 기록"); showcase.LayoutVersion=11;
            AssetDatabase.SaveAssets();
        }
    }
}
