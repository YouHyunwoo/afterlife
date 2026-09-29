using System.Collections.Generic;
using Afterlife.Modules.WorldAudio;
using Afterlife.Modules.AmbientAudio;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeAudio
    {
        [Header("환경음 채널 · AudioRoot 프리팹")]
        [SerializeField] private AudioSource _ambientGrass, _ambientBird, _worldImpact, _worldBreak;
        [SerializeField] private AudioSource[] _waterVoices = new AudioSource[3];
        private WorldAudioPlayer _environmentWater;
        private AmbientAudioController _environmentAmbience;
        private readonly AmbientAudioSettings _environmentSettings=new AmbientAudioSettings();
        private bool _environmentActive;
        private void InitializeEnvironmentAudio()
        {
            if (_ambientGrass == null || _ambientBird == null || _worldImpact == null || _worldBreak == null ||
                _waterVoices == null || _waterVoices.Length != 3 || System.Array.Exists(_waterVoices, source => source == null))
                throw new System.InvalidOperationException("PrototypeAudio: assign all seven environment channels in AudioRoot.");
            var bank=_settings.Audio!=null&&_settings.Audio.WorldBank!=null?_settings.Audio.WorldBank:Resources.Load<WorldAudioBank>("WorldAudio/WorldAudioBank");
            if(bank==null)throw new System.InvalidOperationException("Assign WorldAudioBank in Audio settings or Resources/WorldAudio.");
            SetResourceCue(SoundCue.Wood,bank.Volume,.12f,bank.WoodPitchVariation,false,bank.WoodImpact);
            SetResourceCue(SoundCue.Stone,bank.Volume,.12f,0,true,bank.StoneImpacts);
            SetResourceCue(SoundCue.TreeFall,bank.Volume,.2f,0,false,bank.WoodFall);
            SetResourceCue(SoundCue.StoneBreak,bank.Volume,.2f,0,false,bank.StoneBreak);
            _environmentWater=new WorldAudioPlayer(_worldImpact,_worldBreak,_waterVoices,bank,Resources.Load<AudioClip>("WorldAudio/freesori_water_ripple"),Resources.LoadAll<AudioClip>("WorldAudio/WaterRipples"));
            _environmentSettings.BirdMinimumGap=_settings.Audio!=null?_settings.Audio.BirdMinimumGap:9;
            _environmentSettings.BirdMaximumGap=_settings.Audio!=null?_settings.Audio.BirdMaximumGap:23;
            _environmentAmbience=new AmbientAudioController(_ambientGrass,_ambientBird,_environmentSettings,_settings.Audio!=null&&_settings.Audio.GrassRustles!=null&&_settings.Audio.GrassRustles.Length>0?_settings.Audio.GrassRustles:new[]{
                Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_02"),
                Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_03"),
                Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_04")},_settings.Audio!=null&&_settings.Audio.Bird!=null?_settings.Audio.Bird:Resources.Load<AudioClip>("AmbientSounds/bird"));
        }
        private void SetResourceCue(SoundCue key,float volume,float cooldown,float pitch,bool sequential,params AudioClip[] clips)
        {
            var valid=new List<AudioClip>();if(clips!=null)foreach(var clip in clips)if(clip!=null)valid.Add(clip);
            if(valid.Count==0){Debug.LogWarning("Prototype resource audio missing: "+key,this);return;}
            _cues[key]=new Cue{Clips=valid.ToArray(),Volume=volume,Cooldown=cooldown,PitchVariation=pitch,Sequential=sequential};
        }
        public void TickEnvironment(bool active,bool brushingGrass)
        {
            _environmentActive=active && isActiveAndEnabled && !_model.Ended && Application.isFocused && _settings.SoundVolume>0;
            float volume=Mathf.Clamp01(_settings.SoundVolume);
            _environmentSettings.GrassVolume=(_settings.Audio!=null?_settings.Audio.GrassVolume:.20f)*volume;_environmentSettings.BirdVolume=(_settings.Audio!=null?_settings.Audio.BirdVolume:.38f)*volume;
            _environmentWater.MasterVolume=volume;
            if(!_environmentActive)_environmentWater.Reset();
            _environmentAmbience.Tick(Time.unscaledDeltaTime,_environmentActive,_model.NightBlend<.5f,brushingGrass);
        }
        public void PlayWaterRipple()
        {
            if(_environmentActive && isActiveAndEnabled)_environmentWater.PlayWaterRipple(Time.unscaledTime);
        }
    }
}
