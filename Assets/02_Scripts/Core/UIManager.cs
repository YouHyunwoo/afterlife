using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임 내 모든 UI 화면을 전환 및 관리하는 UIManager
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        public UI.Screen TitleScreen;
        public UI.Screen MainScreen;
        public UI.Screen InGameScreen;
        public UI.Screen ClearScreen; // 클리어 화면
        public UI.Screen GameOverScreen; // 게임 오버 화면

        [Header("Controllers")]
        public UI.Title.Controller TitleController;
        public UI.Main.Controller MainController;
        public UI.Stage.Controller StageController;

        [Header("Transition")]
        [SerializeField] float transitionDuration = 0.5f;
        [SerializeField] Image transitionImage;

        public void Show(GameState state)
        {
            HideAll();
            switch (state)
            {
                case GameState.Title:
                    TitleScreen.Show();
                    break;
                case GameState.Main:
                    MainScreen.Show();
                    break;
                case GameState.InGame:
                    InGameScreen.Show();
                    break;
                case GameState.Clear:
                    ClearScreen.Show();
                    break;
                case GameState.GameOver:
                    GameOverScreen.Show();
                    break;
            }
        }

        private void HideAll()
        {
            TitleScreen.Hide();
            MainScreen.Hide();
            InGameScreen.Hide();
            ClearScreen.Hide();
            GameOverScreen.Hide();
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
