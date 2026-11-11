using System;
using UnityEngine;
using Afterlife.Modules.Terrain;
using Afterlife.Modules.AmbientAudio;
namespace Afterlife.Modules.SceneFeatures
{
    [Serializable] public sealed class SceneFeatureSettings
    {
        public FeatureGroup MapEnvironment=new FeatureGroup(SceneFeature.MapGeneration,SceneFeature.Terrain,SceneFeature.Beach,SceneFeature.Coast,SceneFeature.Water,SceneFeature.Grass,SceneFeature.Decorations,SceneFeature.Leaves,SceneFeature.Wind);
        public FeatureGroup InteractionFeedback=new FeatureGroup(SceneFeature.Picking,SceneFeature.TerrainReaction,SceneFeature.ClickParticles,SceneFeature.Tools,SceneFeature.HitFeedback,SceneFeature.DamageMarks,SceneFeature.Destruction,SceneFeature.Snow,SceneFeature.Sound);
        public FeatureGroup Atmosphere=new FeatureGroup(SceneFeature.DayNight,SceneFeature.Vision,SceneFeature.Pixels,SceneFeature.VisionBlur,SceneFeature.Bloom,SceneFeature.Ambience,SceneFeature.Smoke);
        public MapGenerationSettings Map=new MapGenerationSettings();
        public CoastShape CoastShape=CoastShape.Rounded;
        public float WaterHeight=-.12f,FloorHeight=-.85f;
        [Range(0,1)] public float OpenSeaFoam=1;
        public Color[] Palette={new Color(.39f,.55f,.24f),new Color(.40f,.29f,.16f),new Color(.77f,.67f,.44f),new Color(.08f,.3f,.42f),Color.white,new Color(.3f,.47f,.21f),Color.gray};
        [Range(1,10)] public int GrassSamples=7;
        [Range(.1f,1.5f)] public float GrassSize=1;
        [Min(0)] public int DecorationCount=30,LeafCards=2000;
        [Min(0)] public int DecorationRadius;
        public TerrainBiome DecorationBiome=TerrainBiome.Grass;
        public float WindStrength=1;
        public float ToolContactSeconds=.12f,ToolSwingSeconds=.285f,HitShakeStrength=.2f;
        public float VisionHeight=.8f,CursorRadius=3.2f,SourceRadius=3,TrailSeconds=1.4f;
        public bool Night;
        public Color NightSky=new Color(.006f,.009f,.022f),NightSun=new Color(.38f,.48f,.78f);
        public float NightIntensity=.28f,DayNightSpeed=2;
        [Range(180,720)] public int PixelHeight=360;
        public AmbientAudioSettings Ambient=new AmbientAudioSettings();
        public bool IsEnabled(SceneFeature f)=>MapEnvironment.Has(f)||InteractionFeedback.Has(f)||Atmosphere.Has(f);
        public void Set(SceneFeature feature,bool enabled) { foreach(var group in new[]{MapEnvironment,InteractionFeedback,Atmosphere})foreach(var item in group.Features)if(item.Feature==feature)item.Enabled=enabled; }
        public SceneFeatureSettings Copy()=>JsonUtility.FromJson<SceneFeatureSettings>(JsonUtility.ToJson(this));
    }
    [Serializable] public sealed class FeatureOverride { public SceneFeature Feature; public bool Enabled; }
    [Serializable] public sealed class GroupOverride { public bool Override; public bool Enabled=true; }
    [Serializable] public sealed class SceneFeatureOverrides
    {
        public GroupOverride MapEnvironment=new GroupOverride(),InteractionFeedback=new GroupOverride(),Atmosphere=new GroupOverride();
        public FeatureOverride[] Features=new FeatureOverride[0];
        public bool OverrideMap,OverrideVegetation,OverrideFeedback,OverrideAtmosphere;
        public SceneFeatureSettings Values=new SceneFeatureSettings();
        public SceneFeatureSettings Resolve(SceneFeatureSettings source)
        {
            var s=(source??new SceneFeatureSettings()).Copy();var v=Values.Copy();
            if(MapEnvironment.Override)s.MapEnvironment.Enabled=MapEnvironment.Enabled;
            if(InteractionFeedback.Override)s.InteractionFeedback.Enabled=InteractionFeedback.Enabled;
            if(Atmosphere.Override)s.Atmosphere.Enabled=Atmosphere.Enabled;
            foreach(var f in Features)s.Set(f.Feature,f.Enabled);
            if(OverrideMap){s.Map=v.Map;s.CoastShape=v.CoastShape;s.WaterHeight=v.WaterHeight;s.FloorHeight=v.FloorHeight;s.OpenSeaFoam=v.OpenSeaFoam;s.Palette=v.Palette;}
            if(OverrideVegetation){s.GrassSamples=v.GrassSamples;s.GrassSize=v.GrassSize;s.DecorationCount=v.DecorationCount;s.DecorationRadius=v.DecorationRadius;s.DecorationBiome=v.DecorationBiome;s.LeafCards=v.LeafCards;s.WindStrength=v.WindStrength;}
            if(OverrideFeedback){s.ToolContactSeconds=v.ToolContactSeconds;s.ToolSwingSeconds=v.ToolSwingSeconds;s.HitShakeStrength=v.HitShakeStrength;}
            if(OverrideAtmosphere){s.VisionHeight=v.VisionHeight;s.CursorRadius=v.CursorRadius;s.SourceRadius=v.SourceRadius;s.TrailSeconds=v.TrailSeconds;s.Night=v.Night;s.NightSky=v.NightSky;s.NightSun=v.NightSun;s.NightIntensity=v.NightIntensity;s.DayNightSpeed=v.DayNightSpeed;s.PixelHeight=v.PixelHeight;s.Ambient=v.Ambient;}
            return s;
        }
    }
    [CreateAssetMenu(menuName="Afterlife/Scene Feature Profile")]
    public sealed class SceneFeatureProfile : ScriptableObject
    {
        public SceneFeatureSettings Settings=new SceneFeatureSettings();
        public Material TerrainMaterial,ParticleMaterial;
        public Shader WaterShader,GrassShader,LeafShader;
        public GameObject ChipPrefab;
        public ParticleSystem SmokePrefab;
        public GameObject[] DecorationPrefabs=new GameObject[0];
        public Afterlife.Modules.WorldAudio.WorldAudioBank AudioBank;
        public AudioClip[] GrassSounds=new AudioClip[0];
        public AudioClip Bird;
        public Afterlife.Modules.Destruction.DestructionProfile Destruction;
        public UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset Pipeline;
        public static SceneFeatureSettings AfterSettings()
        {
            var s=new SceneFeatureSettings();foreach(SceneFeature f in Enum.GetValues(typeof(SceneFeature)))s.Set(f,true);
            // Day01 After starts in daylight with the vision demonstration off.
            s.Set(SceneFeature.Vision,false);s.Set(SceneFeature.VisionBlur,false);s.Map.Shape=MapShape.Continent;s.Map.Radius=7;s.GrassSamples=7;
            return s;
        }
    }
}
