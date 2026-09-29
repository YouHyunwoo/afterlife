using UnityEngine;
using System;
using System.Collections.Generic;
namespace Afterlife.Prototype
{
    [Serializable] public sealed class PrototypeSoundData
    {
        public SoundCue Id;
        [Range(0,1)] public float Volume;
        [Min(0)] public float Cooldown;
        public bool Important, Sequential;
        [Range(0,1)] public float PitchVariation;
        public AudioClip[] Clips=Array.Empty<AudioClip>();
        [HideInInspector] public string[] ResourcePaths=Array.Empty<string>();
    }
    [CreateAssetMenu(menuName="Afterlife/Settings/Audio")]
    public sealed class PrototypeAudioSettings : ScriptableObject
    {
        public float GrassVolume=.20f, BirdVolume=.38f;
        public float BirdMinimumGap=9, BirdMaximumGap=23;
        public Afterlife.Modules.WorldAudio.WorldAudioBank WorldBank;
        public AudioClip[] GrassRustles;
        public AudioClip Bird;
        public List<PrototypeSoundData> Cues=Defaults();
        public static List<PrototypeSoundData> Defaults()=>new List<PrototypeSoundData>
        {
            new PrototypeSoundData { Id=SoundCue.Wood,Volume=.75f,Cooldown=.12f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/01B","AfterlifePrototype/Audio/01C"} },
            new PrototypeSoundData { Id=SoundCue.ResourceEmpty,Volume=.65f,Cooldown=.20f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/03A"} },
            new PrototypeSoundData { Id=SoundCue.Gold,Volume=.55f,Cooldown=.35f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/04A"} },
            new PrototypeSoundData { Id=SoundCue.Place,Volume=.75f,Cooldown=.12f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/05A"} },
            new PrototypeSoundData { Id=SoundCue.Work,Volume=.65f,Cooldown=.14f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/06B"} },
            new PrototypeSoundData { Id=SoundCue.Complete,Volume=.70f,Cooldown=.22f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/07A"} },
            new PrototypeSoundData { Id=SoundCue.Hit,Volume=.70f,Cooldown=.08f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/09C"} },
            new PrototypeSoundData { Id=SoundCue.Magic,Volume=.70f,Cooldown=.12f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/12B"} },
            new PrototypeSoundData { Id=SoundCue.Kill,Volume=.65f,Cooldown=.16f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/14A"} },
            new PrototypeSoundData { Id=SoundCue.Critical,Volume=.80f,Cooldown=.15f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/15A"} },
            new PrototypeSoundData { Id=SoundCue.BuildingHit,Volume=.20f,Cooldown=.75f,Important=false,ResourcePaths=new[]{"AfterlifePrototype/Audio/16B"} },
            new PrototypeSoundData { Id=SoundCue.Collapse,Volume=.80f,Cooldown=.25f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/16B"} },
            new PrototypeSoundData { Id=SoundCue.Chest,Volume=.80f,Cooldown=.25f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/17B"} },
            new PrototypeSoundData { Id=SoundCue.Relic,Volume=.80f,Cooldown=.25f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/18A"} },
            new PrototypeSoundData { Id=SoundCue.Select,Volume=.60f,Cooldown=.06f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/19A"} },
            new PrototypeSoundData { Id=SoundCue.Cancel,Volume=.65f,Cooldown=.10f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/20A"} },
            new PrototypeSoundData { Id=SoundCue.Open,Volume=.75f,Cooldown=.12f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/21C"} },
            new PrototypeSoundData { Id=SoundCue.Error,Volume=.75f,Cooldown=.20f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/22A"} },
            new PrototypeSoundData { Id=SoundCue.Warning,Volume=.65f,Cooldown=.50f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/23C"} },
            new PrototypeSoundData { Id=SoundCue.Day,Volume=.70f,Cooldown=.50f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/24C"} },
            new PrototypeSoundData { Id=SoundCue.Result,Volume=.80f,Cooldown=.50f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/25B"} },
            new PrototypeSoundData { Id=SoundCue.Rebirth,Volume=.70f,Cooldown=.50f,Important=true,ResourcePaths=new[]{"AfterlifePrototype/Audio/26C"} },
        };
        public float SoundVolume = .7f;
    }
}
