using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView : IPrototypeGameSession
    {
        [Moonstone.Arc.DependencyInjection.Inject] private PrototypeGameFlow _gameFlow = null; // Moonstone 주입
        private PrototypeFlowScreen _flowScreen;
        private CanvasGroup _gameplayGroup;
        private float _originalGameplayAlpha;
        private bool _originalGameplayInteractable, _originalGameplayRaycasts;
        private bool _runEndPending;

        private void InitializeGameFlow()
        {
            if (_gameFlow == null || _sceneUi == null || _sceneUi.Canvas == null)
            { Debug.LogError("게임 흐름 또는 Canvas 참조가 없습니다."); _ready = false; return; }
            _gameplayGroup = _sceneUi.Canvas.GetComponent<CanvasGroup>();
            if (_gameplayGroup == null) _gameplayGroup = _sceneUi.Canvas.gameObject.AddComponent<CanvasGroup>();
            _originalGameplayAlpha = _gameplayGroup.alpha;
            _originalGameplayInteractable = _gameplayGroup.interactable;
            _originalGameplayRaycasts = _gameplayGroup.blocksRaycasts;
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            _flowScreen = new PrototypeFlowScreen(transform, _font);
            _flowScreen.Requested += OnFlowRequested;
            _gameFlow.ScreenChanged += OnFlowScreenChanged;
            _gameFlow.Initialize();
        }

        private void OnFlowRequested(GameScreen screen)
        {
            if (screen != _gameFlow.Current) return;
            if (screen == GameScreen.Title) _gameFlow.StartGame();
            else if (screen == GameScreen.Intro) _gameFlow.CompleteIntro();
            else _gameFlow.ReturnToTitle();
        }

        private void OnFlowScreenChanged(GameScreen screen)
        {
            bool playing = screen == GameScreen.Playing;
            _camera.enabled = playing;
            if (screen == GameScreen.Title || screen == GameScreen.Intro) _audio.enabled = false;
            _gameplayGroup.alpha = playing ? _originalGameplayAlpha : 0;
            _gameplayGroup.interactable = playing && _originalGameplayInteractable;
            _gameplayGroup.blocksRaycasts = playing && _originalGameplayRaycasts;
            _flowScreen.Show(screen, _gameFlow.Result);
            _uiRefresh = 0;
        }

        void IPrototypeGameSession.Begin()
        {
            _runEndPending = false;
            _audio.enabled = true;
            _model.Reset();
            RebuildVillageView();
            _dragging = false;
            _shownDaySequence = -1;
            _shownModal = Modal.None; _shownModalDepth = 0;
            ResetGoldFeedback();
        }

        void IPrototypeGameSession.Stop()
        {
            _runEndPending = false;
            _simulationTime = 0;
            _model.UpdatePointer(default, false, false);
            _model.ClearMode();
            _pressActive = _dragging = _pointerValid = _orbiting = false;
            _cameraVelocity = Vector3.zero;
            _modal = Modal.None; _modalBody = 0; _modalStack.Clear();
            ClearVillageView();
            if (_dayBannerAnimation != null) _dayBannerAnimation.Hide();
            if (_sceneUi != null)
            {
                _sceneUi.Show("ModalLayer", false);
                if (_sceneUi.ModalBackdrop != null) _sceneUi.ModalBackdrop.SetVisible(false, null, 0, false);
            }
        }

        private void CompletePendingRun()
        {
            if (!_runEndPending) return;
            _runEndPending = false;
            var result = _model.LastResult;
            if (result != null) _gameFlow.Complete(new GameFlowResult(result.Victory, result.BestCleared, result.Rebirths, result.Kills));
        }

        private void DisposeGameFlow()
        {
            if (_gameFlow != null)
            {
                _gameFlow.ScreenChanged -= OnFlowScreenChanged;
                _gameFlow.Dispose();
            }
            if (_flowScreen != null) { _flowScreen.Requested -= OnFlowRequested; _flowScreen.Dispose(); }
            if (_gameplayGroup != null)
            {
                _gameplayGroup.alpha = _originalGameplayAlpha;
                _gameplayGroup.interactable = _originalGameplayInteractable;
                _gameplayGroup.blocksRaycasts = _originalGameplayRaycasts;
            }
        }
    }
}
