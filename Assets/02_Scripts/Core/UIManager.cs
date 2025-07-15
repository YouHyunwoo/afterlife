using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임 내 모든 UI 화면을 전환 및 관리하는 UIManager
    /// </summary>
    public class UIManager : ManagerBase
    {
        [Header("Screens")]
        public UI.Screen TitleScreen;
        public UI.Screen IntroductionScreen;
        public UI.Screen MainScreen;
        public UI.Screen InGameScreen;
        public UI.Screen GameOverScreen;
        public UI.Screen DemoScreen;

        [Header("Controllers")]
        public UI.Title.Controller TitleController;
        public UI.Introduction.Controller IntroductionController;
        public UI.Main.Controller MainController;
        public UI.Stage.Controller StageController;
        public UI.GameOver.Controller GameOverController;
        public UI.Demo.Controller DemoController;

        [Header("Transition")]
        [SerializeField] float transitionDuration = 0.5f;
        [SerializeField] Image transitionImage;

        public override void SetUp()
        {
            TitleController.SetUp();
            IntroductionController.SetUp();
            MainController.SetUp();
            StageController.SetUp();
            GameOverController.SetUp();
            DemoController.SetUp();
            HideAll();
        }

        public override void TearDown()
        {
            TitleController.TearDown();
            IntroductionController.TearDown();
            MainController.TearDown();
            StageController.TearDown();
            GameOverController.TearDown();
            DemoController.TearDown();
        }

        public void HideAll()
        {
            TitleScreen.Hide();
            IntroductionScreen.Hide();
            MainScreen.Hide();
            InGameScreen.Hide();
            GameOverScreen.Hide();
            DemoScreen.Hide();
        }

        public void FadeOut(Action onComplete = null)
        {
            transitionImage.gameObject.SetActive(true);
            transitionImage.color = Color.clear;
            transitionImage.DOFade(1f, transitionDuration).OnComplete(() =>
            {
                transitionImage.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public void FadeIn(Action onComplete = null)
        {
            transitionImage.gameObject.SetActive(true);
            transitionImage.color = Color.black;
            transitionImage.DOFade(0f, transitionDuration).OnComplete(() =>
            {
                transitionImage.gameObject.SetActive(false);
                onComplete?.Invoke();
            });
        }

        public void FadeTransition(Action onAction = null)
        {
            FadeOut(() => { onAction?.Invoke(); FadeIn(); });
        }
    }
}
