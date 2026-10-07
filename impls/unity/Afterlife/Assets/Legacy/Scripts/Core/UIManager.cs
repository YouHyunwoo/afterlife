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
        [SerializeField] UI.Screen[] screens;

        [Header("Transition")]
        [SerializeField] float transitionDuration = 0.5f;
        [SerializeField] Image transitionImage;

        public override void SetUp()
        {
            foreach (var screen in screens)
            {
                screen.ObtainController();

                ServiceLocator.Register(screen);
                ServiceLocator.Register(screen.Controller);
            }

            foreach (var screen in screens)
            {
                screen.Controller.SetUp();
                screen.Hide();
            }
        }

        public override void TearDown()
        {
            foreach (var screen in screens)
            {
                screen.Controller.TearDown();
                screen.Hide();
            }

            foreach (var screen in screens)
            {
                ServiceLocator.Unregister(screen);
                ServiceLocator.Unregister(screen.Controller);
            }
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

        public void Fade(Action onAction = null)
        {
            FadeOut(() => { onAction?.Invoke(); FadeIn(); });
        }
    }
}
