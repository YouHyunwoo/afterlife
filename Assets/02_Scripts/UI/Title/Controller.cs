using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Title
{
    public class Controller : UI.Controller
    {
        const string MasterVolumeKey = "MasterVolume";
        const string BGMVolumeKey = "BGMVolume";
        const string SFXVolumeKey = "SFXVolume";

        Screen titleScreen;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            ServiceLocator.Get<AudioManager>().PlayBGM(SceneState.Title);
        }

        public override void SetUp()
        {
            titleScreen = Screen as Title.Screen;

            Localization.OnLanguageChangedEvent += titleScreen.Localize;
            titleScreen.Localize();

            titleScreen.OnNewGameButtonClickedEvent += OnNewGameButtonClicked;
            titleScreen.OnSettingsButtonClickedEvent += OnSettingsButtonClicked;
            titleScreen.OnExitButtonClickedEvent += OnExitButtonClicked;

            titleScreen.SettingsView.OnMasterVolumeChangedEvent += OnMasterVolumeChanged;
            titleScreen.SettingsView.OnBGMVolumeChangedEvent += OnBGMVolumeChanged;
            titleScreen.SettingsView.OnSFXVolumeChangedEvent += OnSFXVolumeChanged;
            titleScreen.SettingsView.OnLanguageButtonClickedEvent += OnLanguageButtonClicked;

            float master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            float bgm = PlayerPrefs.GetFloat(BGMVolumeKey, 1f);
            float sfx = PlayerPrefs.GetFloat(SFXVolumeKey, 1f);

            titleScreen.SettingsView.SetMasterVolume(master);
            titleScreen.SettingsView.SetBGMVolume(bgm);
            titleScreen.SettingsView.SetSFXVolume(sfx);

            ServiceLocator.Get<AudioManager>().SetMasterVolume(master);
            ServiceLocator.Get<AudioManager>().SetBGMVolume(bgm);
            ServiceLocator.Get<AudioManager>().SetSFXVolume(sfx);
        }

        public override void TearDown()
        {
            titleScreen.OnNewGameButtonClickedEvent -= OnNewGameButtonClicked;
            titleScreen.OnSettingsButtonClickedEvent -= OnSettingsButtonClicked;
            titleScreen.OnExitButtonClickedEvent -= OnExitButtonClicked;

            titleScreen.SettingsView.OnMasterVolumeChangedEvent -= OnMasterVolumeChanged;
            titleScreen.SettingsView.OnBGMVolumeChangedEvent -= OnBGMVolumeChanged;
            titleScreen.SettingsView.OnSFXVolumeChangedEvent -= OnSFXVolumeChanged;
            titleScreen.SettingsView.OnLanguageButtonClickedEvent -= OnLanguageButtonClicked;

            Localization.OnLanguageChangedEvent -= titleScreen.Localize;

            titleScreen = null;
        }

        void OnMasterVolumeChanged(float volume)
        {
            ServiceLocator.Get<AudioManager>().SetMasterVolume(volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, volume);
        }

        void OnBGMVolumeChanged(float volume)
        {
            ServiceLocator.Get<AudioManager>().SetBGMVolume(volume);
            PlayerPrefs.SetFloat(BGMVolumeKey, volume);
        }

        void OnSFXVolumeChanged(float volume)
        {
            ServiceLocator.Get<AudioManager>().SetSFXVolume(volume);
            PlayerPrefs.SetFloat(SFXVolumeKey, volume);
        }

        void OnNewGameButtonClicked()
        {
            ServiceLocator.Get<UIManager>().Fade(() =>
            {
                ServiceLocator.Get<SceneManager>().ChangeState(SceneState.Introduction);
            });
        }
        void OnSettingsButtonClicked() => titleScreen.SettingsView.Show();
        void OnExitButtonClicked() => ServiceLocator.Get<ApplicationManager>().Quit();

        void OnLanguageButtonClicked()
        {
            var rotatedLanguage = (int)(Localization.CurrentLanguage + 1) % Enum.GetValues(typeof(Language)).Length;
            Localization.SetLanguage((Language)rotatedLanguage);
        }
    }
}