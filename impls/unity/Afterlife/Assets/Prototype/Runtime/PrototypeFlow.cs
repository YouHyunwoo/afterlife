using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView : IPrototypeGameSession
    {
        [Moonstone.Arc.DependencyInjection.Inject] private PrototypeGameFlow _gameFlow = null; // Moonstone 주입
        [SerializeField] private PrototypeFlowScreen _flowScreen;
        private CanvasGroup _gameplayGroup;
        private float _originalGameplayAlpha;
        private bool _originalGameplayInteractable, _originalGameplayRaycasts;
        private bool _runEndPending;

        private void InitializeGameFlow()
        {
            if (_gameFlow == null || _sceneUi == null || _sceneUi.Canvas == null || _flowScreen == null)
            { Debug.LogError("게임 흐름, Canvas 또는 Game Flow Screens 참조가 없습니다."); _ready = false; return; }
            _gameplayGroup = _sceneUi.Canvas.GetComponent<CanvasGroup>();
            if (_gameplayGroup == null)
            { Debug.LogError("Gameplay Canvas에 CanvasGroup이 없습니다.", _sceneUi.Canvas); _ready = false; return; }
            _originalGameplayAlpha = _gameplayGroup.alpha;
            _originalGameplayInteractable = _gameplayGroup.interactable;
            _originalGameplayRaycasts = _gameplayGroup.blocksRaycasts;
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont(new[] { "Malgun Gothic", "Arial" }, 24);
            _flowScreen.ShowRebirth = !_model.Settings.MvpSlots;
            if (!_flowScreen.Initialize(_font)) { _ready = false; return; }
            _flowScreen.Requested += OnFlowRequested;
            _flowScreen.CommandRequested += OnFlowCommand;
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

        private void OnFlowCommand(PrototypeFlowCommand command)
        {
            switch (command)
            {
                case PrototypeFlowCommand.Start: _gameFlow.StartGame(); break;
                case PrototypeFlowCommand.CompleteIntro: _gameFlow.CompleteIntro(); break;
                case PrototypeFlowCommand.Retry: _gameFlow.Retry(); break;
                case PrototypeFlowCommand.ReturnToTitle: _gameFlow.ReturnToTitle(); break;
                case PrototypeFlowCommand.Quit: Application.Quit(); break;
            }
        }

        private void OnFlowScreenChanged(GameScreen screen)
        {
            bool playing = screen == GameScreen.Playing;
            if (!playing) CancelPointerWork();
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
            ReloadUiPreferences();
            _audio.enabled = true;
            _model.Settings.MapSeed=_settings.MapSeed;
            MeasureStartup("ModelReset", _model.Reset);
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
            _nightCleanupMarkers?.Clear();
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
                if (_sceneUi.ModalBackdrop != null) _sceneUi.ModalBackdrop.HideImmediate();
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
            if (_flowScreen != null) { _flowScreen.Requested -= OnFlowRequested; _flowScreen.CommandRequested -= OnFlowCommand; _flowScreen.Unbind(); }
            if (_gameplayGroup != null)
            {
                _gameplayGroup.alpha = _originalGameplayAlpha;
                _gameplayGroup.interactable = _originalGameplayInteractable;
                _gameplayGroup.blocksRaycasts = _originalGameplayRaycasts;
            }
        }
    }
}
