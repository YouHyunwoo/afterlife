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

            Localization.OnLanguageChangedEvent += introductionScreen.Localize;
            introductionScreen.Localize();

            pageCount = 6;
            currentPageIndex = 0;
        }

        public override void TearDown()
        {
            Localization.OnLanguageChangedEvent -= introductionScreen.Localize;

            introductionScreen = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                currentPageIndex++;
                if (currentPageIndex < pageCount)
                {
                    RefreshMessage();
                }
                else
                {
                    ServiceLocator.Get<GameManager>().StartGame();
                    enabled = false;
                }
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                ServiceLocator.Get<GameManager>().StartGame();
                enabled = false;
            }
        }

        void RefreshMessage()
        {
            if (currentPageIndex < pageCount)
            {
                var message = Localization.Get($"introduction.messages.{currentPageIndex}");
                introductionScreen.SetMessage(message);
            }
            else
            {
                Debug.Log("End of introduction.");
            }
        }
    }
}