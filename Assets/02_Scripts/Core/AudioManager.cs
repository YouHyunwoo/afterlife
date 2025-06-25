using UnityEngine;
using System.Collections.Generic;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임 내 오디오(BGM, 효과음) 재생 및 관리를 담당하는 AudioManager
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("Audio Sources")]
        public AudioSource bgmSource;
        public AudioSource sfxSource;

        [Header("BGM Clips")]
        public AudioClip titleBGM;
        public AudioClip mainBGM;
        public AudioClip inGameBGM;
        public AudioClip gameOverBGM;

        [Header("SFX Clips")] // 효과음은 Dictionary로 관리 예시
        public List<AudioClip> sfxClips;
        private Dictionary<string, AudioClip> sfxClipDict = new();

        void Awake()
        {
            foreach (var clip in sfxClips)
            {
                if (clip != null && !sfxClipDict.ContainsKey(clip.name))
                    sfxClipDict.Add(clip.name, clip);
            }
        }

        public void PlayBGM(GameState state)
        {
            AudioClip clip = null;
            switch (state)
            {
                case GameState.Title:
                    clip = titleBGM;
                    break;
                case GameState.Main:
                    clip = mainBGM;
                    break;
                case GameState.InGame:
                    clip = inGameBGM;
                    break;
                case GameState.GameOver:
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
