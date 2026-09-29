using System;
using UnityEngine;

namespace Afterlife.Modules.WorldAudio
{
    // 타격 피치 변화가 이미 재생 중인 쓰러짐 소리에 전파되지 않도록 채널을 분리한다.
    public sealed class WorldAudioPlayer : IDisposable
    {
        private readonly WorldAudioBank _bank;
        private readonly AudioSource _impact,_break;
        private readonly AudioSource[] _water=new AudioSource[3];
        private readonly bool _ownsSources;
        private readonly AudioClip[] _waterRipples;
        private readonly System.Random _waterRandom=new System.Random();
        private float _lastWaterRequest=float.NegativeInfinity, _nextWaterTime=float.NegativeInfinity;
        private int _lastWaterIndex=-1;
        private int _stoneIndex;
        public float MasterVolume
        {
            set
            {
                float gain=Mathf.Clamp01(value);_impact.volume=gain;_break.volume=gain;
                foreach(var source in _water)source.volume=gain;
            }
        }
        public WorldAudioPlayer(Transform parent,WorldAudioBank bank,AudioClip waterRipple=null,AudioClip[] waterVariants=null)
            : this(Make(parent,"ImpactAudio"),Make(parent,"BreakAudio"),new[]{Make(parent,"WaterRippleAudio0"),Make(parent,"WaterRippleAudio1"),Make(parent,"WaterRippleAudio2")},bank,waterRipple,waterVariants,true) { }

        public WorldAudioPlayer(AudioSource impact,AudioSource breakSource,AudioSource[] water,WorldAudioBank bank,AudioClip waterRipple=null,AudioClip[] waterVariants=null)
            : this(impact,breakSource,water,bank,waterRipple,waterVariants,false) { }

        private WorldAudioPlayer(AudioSource impact,AudioSource breakSource,AudioSource[] water,WorldAudioBank bank,AudioClip waterRipple,AudioClip[] waterVariants,bool ownsSources)
        {
            if(impact==null||breakSource==null||water==null||water.Length!=3||Array.Exists(water,source=>source==null))
                throw new ArgumentException("Assign impact, break, and three water AudioSources.");
            _bank=bank;
            _impact=impact; _break=breakSource; _ownsSources=ownsSources;
            for (int i=0;i<_water.Length;i++) _water[i]=water[i];
            if(_ownsSources){Configure(_impact); Configure(_break); foreach(var source in _water)Configure(source);}
            var clips=new System.Collections.Generic.List<AudioClip>();
            var variants=bank!=null && bank.WaterRippleVariants!=null && bank.WaterRippleVariants.Length>0
                ? bank.WaterRippleVariants : waterVariants;
            if (variants!=null) foreach (var clip in variants)
                if (clip!=null && !clips.Contains(clip)) clips.Add(clip);
            // 명시적으로 설정한 기존 단일 클립도 유지한다.
            if (bank!=null && bank.WaterRipple!=null &&
                (bank.WaterRippleVariants==null || bank.WaterRippleVariants.Length==0))
            { clips.Clear(); clips.Add(bank.WaterRipple); }
            if (clips.Count==0 && waterRipple!=null) clips.Add(waterRipple);
            _waterRipples=clips.ToArray();
            if (_waterRipples.Length==0) Debug.LogWarning("Afterlife: water ripple audio clip is missing. Check the Audio Bank or Resources import.",_impact);
        }
        private static void Configure(AudioSource source) { source.playOnAwake=false; source.spatialBlend=0; }
        private static AudioSource Make(Transform parent,string name)
        {
            var go=new GameObject(name); go.transform.SetParent(parent,false);
            var audio=go.AddComponent<AudioSource>(); audio.playOnAwake=false; audio.spatialBlend=0;
            return audio;
        }
        public void Play(bool wood,bool broken,float time)
        {
            if (_bank==null) return;
            AudioClip clip;
            if (broken) clip=wood ? _bank.WoodFall : _bank.StoneBreak;
            else if (wood) clip=_bank.WoodImpact;
            else
            {
                if (_bank.StoneImpacts==null || _bank.StoneImpacts.Length==0) return;
                clip=_bank.StoneImpacts[_stoneIndex%_bank.StoneImpacts.Length];
                _stoneIndex=(_stoneIndex+1)%_bank.StoneImpacts.Length;
            }
            var audio=broken ? _break : _impact;
            audio.pitch=wood && !broken ? 1+Mathf.Sin(time*7)*_bank.WoodPitchVariation : 1;
            if (clip!=null) audio.PlayOneShot(clip,_bank.Volume);
        }
        public void PlayWaterRipple(float time)
        {
            bool first=time-_lastWaterRequest>.6f;
            _lastWaterRequest=time;
            if (_waterRipples.Length==0 || time<_nextWaterTime) return;
            AudioSource voice=null;
            foreach (var candidate in _water) if (!candidate.isPlaying) { voice=candidate; break; }
            if (voice==null) return;
            int index=_waterRandom.Next(_waterRipples.Length);
            if (_waterRipples.Length>1 && index==_lastWaterIndex)
                index=(index+1+_waterRandom.Next(_waterRipples.Length-1))%_waterRipples.Length;
            _lastWaterIndex=index;
            _nextWaterTime=time+.25f+(float)_waterRandom.NextDouble()*.15f;
            // 음성마다 피치를 고정해 이전 소리의 여운이 변하지 않게 한다.
            voice.pitch=.98f+(float)_waterRandom.NextDouble()*.04f;
            float volume=(_bank!=null ? _bank.WaterRippleVolume : .525f)*(first ? 1 : .55f);
            voice.PlayOneShot(_waterRipples[index],volume*(.92f+(float)_waterRandom.NextDouble()*.16f));
        }
        public void Reset()
        {
            _stoneIndex=0; _lastWaterIndex=-1;
            _lastWaterRequest=float.NegativeInfinity; _nextWaterTime=float.NegativeInfinity;
            _impact.Stop(); _break.Stop();
            foreach (var voice in _water) voice.Stop();
        }
        public void Dispose()
        {
            Reset();
            if (!_ownsSources) return;
            Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_impact.gameObject); Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_break.gameObject);
            foreach (var voice in _water) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(voice.gameObject);
        }
    }
}
