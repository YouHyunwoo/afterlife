using UnityEngine;

namespace Afterlife.Prototype
{
    // 촬영 도구가 호출하는 최소 어댑터. 대본/샷/Devlog 타입은 게임이 알지 않는다.
    public sealed partial class PrototypeView
    {
        public bool CaptureActive { get; private set; }
        public bool CaptureInputAllowed { get; private set; }
        public bool CaptureReady => _ready && _camera != null && _gameFlow != null;
        public Camera CaptureCamera => _camera;
        private int _captureExitFrame = -1;
        private bool CaptureOwnsInput => CaptureActive || _captureExitFrame == Time.frameCount;
        private float _captureTimeScale, _captureCameraSize;
        private bool _captureListenerPaused, _captureUiInteractable, _captureUiRaycasts;
        private Vector3 _captureCameraPosition;
        private Quaternion _captureCameraRotation;

        public bool BeginCapture()
        {
            if (!CaptureReady || CaptureActive) return false;
            if (!_gameFlow.IsPlaying)
            {
                if (_gameFlow.Current != GameScreen.Title) _gameFlow.ReturnToTitle();
                _gameFlow.StartGame();
                _gameFlow.CompleteIntro();
            }
            if (!_gameFlow.IsPlaying) return false;
            _captureTimeScale = Time.timeScale;
            _captureListenerPaused = AudioListener.pause;
            _captureCameraPosition = _camera.transform.position;
            _captureCameraRotation = _camera.transform.rotation;
            _captureCameraSize = _camera.orthographicSize;
            _captureUiInteractable = _gameplayGroup.interactable;
            _captureUiRaycasts = _gameplayGroup.blocksRaycasts;
            CaptureActive = true;
            _gameplayGroup.interactable = _gameplayGroup.blocksRaycasts = false;
            ResetCaptureTake();
            return true;
        }

        public void ResetCaptureTake()
        {
            if (!CaptureActive) return;
            ((IPrototypeGameSession)this).Begin();
            _shownDaySequence = _model.DaySequence;
            _dayBannerAnimation?.Hide();
            _model.ClearMode();
            _model.UpdatePointer(default, false, false);
            _pressActive = _dragging = _orbiting = false;
            _selectedBuildKind = null;
            _cameraVelocity = Vector3.zero;
            _uiRefresh = 0;
        }

        public void SetCapturePlayback(bool playing, bool allowInteraction)
        {
            if (!CaptureActive) return;
            CaptureInputAllowed = playing && allowInteraction;
            // 필름은 unscaled 시간으로 진행한다. 설명 컷/일시정지에서는 게임과 타격 연출을 함께 정지한다.
            Time.timeScale = CaptureInputAllowed ? 1f : 0f;
            AudioListener.pause = !CaptureInputAllowed;
            if (!CaptureInputAllowed)
            {
                _pressActive = _dragging = false;
                _model.UpdatePointer(default, false, false);
            }
        }

        public void EndCapture()
        {
            if (!CaptureActive) return;
            Time.timeScale = _captureTimeScale;
            AudioListener.pause = _captureListenerPaused;
            CaptureActive = CaptureInputAllowed = false;
            _captureExitFrame = Time.frameCount;
            _simulationTime = 0;
            _pressActive = _dragging = _orbiting = false;
            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_captureCameraPosition, _captureCameraRotation);
                _camera.orthographicSize = _captureCameraSize;
            }
            if (_gameplayGroup != null)
            {
                _gameplayGroup.interactable = _captureUiInteractable;
                _gameplayGroup.blocksRaycasts = _captureUiRaycasts;
            }
        }
    }
}
