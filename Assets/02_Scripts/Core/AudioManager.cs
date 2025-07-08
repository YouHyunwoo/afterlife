using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임 내 오디오(BGM, 효과음) 재생 및 관리를 담당하는 AudioManager
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Mixer")]
        [SerializeField] AudioMixer audioMixer;

        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        [Header("BGM Clips")]
        public AudioClip titleBGM;
        public AudioClip mainBGM;
        public AudioClip inGameBGM;
        public AudioClip gameOverBGM;

        [Header("SFX Clips")]
        public List<AudioClip> sfxClips;
        readonly Dictionary<string, AudioClip> sfxClipDict = new();

        void Awake()
        {
            foreach (var clip in sfxClips)
            {
                if (clip != null && !sfxClipDict.ContainsKey(clip.name))
                    sfxClipDict.Add(clip.name, clip);
            }
        }

        public void SetMasterVolume(float volume)
        {
            audioMixer.SetFloat("Master", Mathf.Log10(volume) * 20);
        }

        public void SetBGMVolume(float volume)
        {
            audioMixer.SetFloat("BGM", Mathf.Log10(volume) * 20);
        }

        public void SetSFXVolume(float volume)
        {
            audioMixer.SetFloat("SFX", Mathf.Log10(volume) * 20);
        }

        public void PlayBGM(SceneState state)
        {
            AudioClip clip = null;
            switch (state)
            {
                case SceneState.Title:
                    clip = titleBGM;
                    break;
                case SceneState.Main:
                    clip = mainBGM;
                    break;
                case SceneState.InGame:
                    clip = inGameBGM;
                    break;
                case SceneState.GameOver:
                    clip = gameOverBGM;
                    break;
            }
            if (bgmSource != null && clip != null)
            {
                bgmSource.clip = clip;
                bgmSource.Stop();
                bgmSource.loop = true;
                bgmSource.Play();
            }
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void PlaySFX(string seName)
        {
            if (sfxSource != null && sfxClipDict.TryGetValue(seName, out var clip))
            {
                sfxSource.PlayOneShot(clip);
            }
        }
    }
}
