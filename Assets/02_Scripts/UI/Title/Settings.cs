using System;
using UnityEngine.UI;

namespace Afterlife.UI.Title
{
    public class Settings : View
    {
        public Slider MasterVolumeSlider;
        public Slider BGMVolumeSlider;
        public Slider SFXVolumeSlider;
        public Button LanguageButton;

        public event Action<float> OnMasterVolumeChangedEvent;
        public event Action<float> OnBGMVolumeChangedEvent;
        public event Action<float> OnSFXVolumeChangedEvent;
        public event Action OnLanguageButtonClickedEvent;

        void Awake()
        {
            MasterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            BGMVolumeSlider.onValueChanged.AddListener(OnBGMVolumeChanged);
            SFXVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            LanguageButton.onClick.AddListener(OnLanguageButtonClicked);
        }

        void OnMasterVolumeChanged(float value) => OnMasterVolumeChangedEvent?.Invoke(value);
        void OnBGMVolumeChanged(float value) => OnBGMVolumeChangedEvent?.Invoke(value);
        void OnSFXVolumeChanged(float value) => OnSFXVolumeChangedEvent?.Invoke(value);
        void OnLanguageButtonClicked() => OnLanguageButtonClickedEvent?.Invoke();

        public void SetMasterVolume(float volume) => MasterVolumeSlider.value = volume;
        public void SetBGMVolume(float volume) => BGMVolumeSlider.value = volume;
        public void SetSFXVolume(float volume) => SFXVolumeSlider.value = volume;
    }
}