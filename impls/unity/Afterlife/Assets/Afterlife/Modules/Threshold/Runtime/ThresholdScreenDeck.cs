using System;
using UnityEngine;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Drop the deck under a Canvas and call Show from a game-specific adapter.</summary>
    [DisallowMultipleComponent]
    public sealed class ThresholdScreenDeck : MonoBehaviour
    {
        [SerializeField] private ThresholdScreenView[] _pages;
        [SerializeField] private ThresholdIntroSequenceView _intro;
        private ThresholdScreen _current;

        public event Action<ThresholdAction> ActionRequested;

        public void Configure(ThresholdScreenView[] pages, ThresholdIntroSequenceView intro)
        {
            _pages = pages;
            _intro = intro;
        }

        private void Awake()
        {
            if (_pages == null || _pages.Length == 0) _pages = GetComponentsInChildren<ThresholdScreenView>(true);
            if (_intro == null) _intro = GetComponentInChildren<ThresholdIntroSequenceView>(true);
        }

        private void OnEnable()
        {
            if (_pages == null || _pages.Length == 0) _pages = GetComponentsInChildren<ThresholdScreenView>(true);
            foreach (var page in _pages) if (page != null) page.ActionRequested += Forward;
            if (_intro != null) _intro.Completed += CompleteIntro;
        }

        private void OnDisable()
        {
            foreach (var page in _pages) if (page != null) page.ActionRequested -= Forward;
            if (_intro != null) _intro.Completed -= CompleteIntro;
        }

        public void Show(ThresholdScreen screen, string resultSummary = null)
        {
            if (_current == ThresholdScreen.Lobby && screen != ThresholdScreen.Lobby && _intro != null) _intro.Stop();
            _current = screen;
            foreach (var page in _pages)
                if (page != null) page.Show(screen != ThresholdScreen.Playing && page.Screen == screen, resultSummary);
            if (screen == ThresholdScreen.Lobby && _intro != null) _intro.Begin();
        }

        private void Forward(ThresholdAction action) => ActionRequested?.Invoke(action);
        private void CompleteIntro() => ActionRequested?.Invoke(ThresholdAction.EnterTrial);
    }
}
