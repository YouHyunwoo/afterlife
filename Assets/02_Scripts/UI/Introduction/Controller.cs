using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI.Introduction
{
    public class Controller : UI.Controller
    {
        Screen introductionScreen;

        int currentPageIndex;
        int pageCount;

        public override void OnSceneEntered(SceneState previousSceneState, UI.Controller previousScreen)
        {
            currentPageIndex = 0;
            RefreshMessage();

            enabled = true;
        }

        public override void OnSceneExited(SceneState nextSceneState, UI.Controller nextScreen)
        {
            enabled = false;
        }

        public override void SetUp()
        {
            introductionScreen = Screen as Introduction.Screen;

            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent += introductionScreen.Localize;
            introductionScreen.Localize();

            introductionScreen.NextButton.onClick.AddListener(NextMessage);

            pageCount = 6;
            currentPageIndex = 0;
        }

        public override void TearDown()
        {
            ServiceLocator.Get<LocalizationManager>().OnLanguageChangedEvent -= introductionScreen.Localize;

            introductionScreen.NextButton.onClick.RemoveListener(NextMessage);

            introductionScreen = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                NextMessage();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                NextScene();
            }
        }

        void RefreshMessage()
        {
            var message = LocalizationManager.Get($"introduction.messages.{currentPageIndex}");
            introductionScreen.SetMessage(message);
        }

        void NextMessage()
        {
            currentPageIndex++;
            if (currentPageIndex < pageCount)
            {
                RefreshMessage();
            }
            else
            {
                NextScene();
            }
        }

        void NextScene()
        {
            ServiceLocator.Get<GameManager>().StartGame();
            enabled = false;
        }
    }
}