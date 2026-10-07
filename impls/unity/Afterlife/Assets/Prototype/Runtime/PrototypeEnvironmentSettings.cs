using System;
using UnityEngine;
namespace Afterlife.Prototype
{
    [Serializable] public sealed class PrototypeEnvironmentValues
    {
        public Color[] TerrainPalette={new Color(.30f,.40f,.23f),new Color(.43f,.34f,.25f),new Color(.68f,.61f,.44f),new Color(.055f,.25f,.32f),new Color(.81f,.85f,.88f),new Color(.23f,.32f,.19f),new Color(.43f,.45f,.43f)};
        public float WaterHeight=-.46f, SeaFloorHeight=-.82f, RuinsGlow=3.5f;
        [Range(1,12)] public int GrassSamplesPerAxis=7;
        [Range(0,1)] public float GrassAccentFraction=.25f;
        [Min(1)] public int LeafCards=384;
        [Min(0)] public float WindStrength=1;
        public float SnowHitRadius=.12f, SnowHitPower=.55f;
        public int SnowParticlesPerHit=20;
    }
    [CreateAssetMenu(menuName="Afterlife/Settings/Environment")]
    public sealed class PrototypeEnvironmentSettings : ScriptableObject
    {
        public PrototypeEnvironmentValues Values=new PrototypeEnvironmentValues();
    }
}
