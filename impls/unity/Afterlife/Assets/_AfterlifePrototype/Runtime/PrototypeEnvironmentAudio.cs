using System.Collections.Generic;
using Afterlife.Devlog.Audio;
using Afterlife.Devlog.AmbientAudio;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeAudio
    {
        private ShowcaseAudio _environmentWater;
        private ShowcaseAmbience _environmentAmbience;
        private ShowcaseAudioBank _ownedAudioBank;
        private readonly AmbientAudioSettings _environmentSettings=new AmbientAudioSettings();
        private bool _environmentActive;
        private void InitializeEnvironmentAudio()
        {
            var bank=Resources.Load<ShowcaseAudioBank>("DevlogAudio/ShowcaseAudioBank");
            if(bank==null)
            {
                bank=_ownedAudioBank=ScriptableObject.CreateInstance<ShowcaseAudioBank>();
                bank.WoodImpact=Resources.Load<AudioClip>("AfterlifePrototype/Audio/01B");
                bank.WoodFall=Resources.Load<AudioClip>("DevlogAudio/gongu_13220749_tree_fall");
                bank.StoneImpacts=new[]{Resources.Load<AudioClip>("DevlogAudio/10_dropping_rocks_impact_17"),Resources.Load<AudioClip>("DevlogAudio/11_dropping_rocks_impact_15")};
                bank.StoneBreak=Resources.Load<AudioClip>("DevlogAudio/dropping_rocks_debris_03");
            }
            SetResourceCue(SoundCue.Wood,bank.Volume,.12f,bank.WoodPitchVariation,false,bank.WoodImpact);
            SetResourceCue(SoundCue.Stone,bank.Volume,.12f,0,true,bank.StoneImpacts);
            SetResourceCue(SoundCue.TreeFall,bank.Volume,.2f,0,false,bank.WoodFall);
            SetResourceCue(SoundCue.StoneBreak,bank.Volume,.2f,0,false,bank.StoneBreak);
            _environmentWater=new ShowcaseAudio(transform,bank,Resources.Load<AudioClip>("DevlogAudio/freesori_water_ripple"),Resources.LoadAll<AudioClip>("DevlogAudio/WaterRipples"));
            _environmentAmbience=new ShowcaseAmbience(transform,_environmentSettings,new[]{
                Resources.Load<AudioClip>("DevlogAmbience/GrassRustles/grass_rustle_02"),
                Resources.Load<AudioClip>("DevlogAmbience/GrassRustles/grass_rustle_03"),
                Resources.Load<AudioClip>("DevlogAmbience/GrassRustles/grass_rustle_04")},Resources.Load<AudioClip>("DevlogAmbience/bird"));
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
            _environmentSettings.GrassVolume=.20f*volume;_environmentSettings.BirdVolume=.38f*volume;
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
