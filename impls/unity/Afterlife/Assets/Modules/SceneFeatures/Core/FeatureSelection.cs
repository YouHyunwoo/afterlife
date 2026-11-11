using System;
namespace Afterlife.Modules.SceneFeatures
{
    public enum SceneFeature { MapGeneration,Terrain,Beach,Coast,Water,Grass,Decorations,Leaves,Wind,Picking,TerrainReaction,ClickParticles,Tools,HitFeedback,DamageMarks,Destruction,Snow,Sound,DayNight,Vision,Pixels,VisionBlur,Bloom,Ambience,Smoke }
    [Serializable] public sealed class FeatureSwitch { public SceneFeature Feature; public bool Enabled; public FeatureSwitch(SceneFeature feature,bool enabled=false){Feature=feature;Enabled=enabled;} }
    [Serializable] public sealed class FeatureGroup
    {
        public bool Enabled=true;
        public FeatureSwitch[] Features;
        public FeatureGroup(params SceneFeature[] features) { Features=Array.ConvertAll(features,f=>new FeatureSwitch(f)); }
        public bool Has(SceneFeature feature) { foreach(var f in Features)if(f.Feature==feature)return Enabled&&f.Enabled;return false; }
    }
}
