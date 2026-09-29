using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Afterlife.Modules.Threshold;
using Afterlife.Modules.Threshold.Unity;

namespace Afterlife.Prototype
{
    public enum PrototypeFlowCommand { Start, CompleteIntro, Retry, ReturnToTitle, Quit }

    // 화면 구조와 스타일은 프리팹이 소유한다. 이 컴포넌트는 상태 표시와 버튼 의도만 전달한다.
    public sealed class PrototypeFlowScreen : MonoBehaviour
    {
        [SerializeField] private Text _title;
        [SerializeField] private Text _body;
        [SerializeField] private Text _actionLabel;
        [SerializeField] private Button _action;
        [Header("ThresholdLab presentation")]
        [SerializeField] private ThresholdScreenDeck _deck;
        [SerializeField] private ThresholdBackdropRig _backdrop;
        [SerializeField] private RawImage _backdropDisplay;
        [SerializeField] private ThresholdLoadingView _loading;
        [SerializeField] private PrototypeTitleSettings _titleSettings;
        private GameScreen _screen;
        private int _shownFrame;
        private bool _initialized;
        private Coroutine _entryRoutine;
        public event Action<GameScreen> Requested;
        public event Action<PrototypeFlowCommand> CommandRequested;

        public void Configure(ThresholdScreenDeck deck, ThresholdBackdropRig backdrop, RawImage display, ThresholdLoadingView loading, PrototypeTitleSettings titleSettings)
        {
            _deck = deck;
            _backdrop = backdrop;
            _backdropDisplay = display;
            _loading = loading;
            _titleSettings = titleSettings;
        }

        public bool Initialize(Font font)
        {
            if (_initialized) return true;
            if (_deck != null)
            {
                if (_backdrop == null || _loading == null)
                {
                    Debug.LogError("Threshold backdrop and loading view references are required.", this);
                    return false;
                }
                _deck.ActionRequested += OnThresholdAction;
                _backdrop.Bind(_backdropDisplay);
                _loading.SetVisible(false);
                if (_titleSettings != null) _titleSettings.Closed += CloseTitleSettings;
                _initialized = true;
                return true;
            }
            if (_title == null || _body == null || _actionLabel == null || _action == null || font == null)
            {
                Debug.LogError("Game Flow Screens prefab references are incomplete.", this);
                return false;
            }
            _title.font = font;
            _body.font = font;
            _actionLabel.font = font;
            _action.onClick.AddListener(OnRequested);
            _initialized = true;
            return true;
        }
        private void OnRequested()
        {
            // 같은 프레임의 중복 클릭이 다음 화면까지 통과하지 않게 한다.
            if (Time.frameCount <= _shownFrame) return;
            Requested?.Invoke(_screen);
        }
        public bool ShowRebirth { get; set; } = true;
        public void Show(GameScreen screen, GameFlowResult result)
        {
            if (_entryRoutine != null && screen != GameScreen.Playing) CancelPendingEntry();
            _screen = screen; _shownFrame = Time.frameCount;
            if (_deck != null)
            {
                CloseTitleSettings();
                var threshold = screen == GameScreen.Title ? ThresholdScreen.Title
                    : screen == GameScreen.Intro ? ThresholdScreen.Lobby
                    : screen == GameScreen.Clear ? ThresholdScreen.Clear
                    : screen == GameScreen.GameOver ? ThresholdScreen.GameOver : ThresholdScreen.Playing;
                string summary = result == null ? string.Empty
                    : $"최고 {result.BestCleared}일 방어  ·  " + (ShowRebirth ? $"환생 {result.Rebirths}회  ·  " : "") + $"처치 {result.Kills}";
                _deck.Show(threshold, summary);
                if (_backdrop != null) _backdrop.Show(threshold);
                gameObject.SetActive(true);
                return;
            }
            gameObject.SetActive(screen != GameScreen.Playing);
            if (screen == GameScreen.Playing) return;
            _title.text = screen == GameScreen.Title ? "AFTERLIFE" : screen == GameScreen.Intro ? "마지막 불빛을 지켜주세요" : screen == GameScreen.Clear ? "마을에 찾아온 평화" : "마지막 불빛이 꺼졌습니다";
            _body.text = screen == GameScreen.Title ? "작은 집 하나에서 시작하는 마을의 이야기" : screen == GameScreen.Intro
                ? "낮에는 자원을 모으고 마을을 키우세요.\n밤에는 다가오는 적으로부터 집을 지키세요.\n마지막 밤까지 살아남으면 마을에 평화가 찾아옵니다."
                : $"{(screen == GameScreen.Clear ? "클리어" : "게임 오버")}\n최고 {result?.BestCleared ?? 0}일 방어 · " + (ShowRebirth ? $"환생 {result?.Rebirths ?? 0}회 · " : "") + $"처치 {result?.Kills ?? 0}\n영구 해금은 다음 판에도 유지됩니다.";
            _actionLabel.text = screen == GameScreen.Title ? "게임 시작" : screen == GameScreen.Intro ? "마을로 들어가기" : "타이틀로";
        }
        public void Unbind()
        {
            CancelPendingEntry();
            if (_deck != null) _deck.ActionRequested -= OnThresholdAction;
            if (_titleSettings != null) _titleSettings.Closed -= CloseTitleSettings;
            if (_initialized && _action != null) _action.onClick.RemoveListener(OnRequested);
            _initialized = false;
            Requested = null;
            CommandRequested = null;
        }
        private void OnThresholdAction(ThresholdAction action)
        {
            if (Time.frameCount <= _shownFrame) return;
            switch (action)
            {
                case ThresholdAction.NewGame:
                    if (_screen == GameScreen.Title) CommandRequested?.Invoke(PrototypeFlowCommand.Start);
                    break;
                case ThresholdAction.EnterTrial:
                    if (_screen == GameScreen.Intro) BeginTrial(PrototypeFlowCommand.CompleteIntro, true);
                    else if (_screen == GameScreen.GameOver) BeginTrial(PrototypeFlowCommand.Retry, false);
                    break;
                case ThresholdAction.Title:
                    CommandRequested?.Invoke(PrototypeFlowCommand.ReturnToTitle);
                    break;
                case ThresholdAction.OpenModal:
                    if (_screen == GameScreen.Title && _titleSettings != null)
                    {
                        _titleSettings.Show();
                        if (_backdrop != null) _backdrop.SetModal(true);
                    }
                    break;
                case ThresholdAction.Quit:
                    CommandRequested?.Invoke(PrototypeFlowCommand.Quit);
                    break;
            }
        }
        private void BeginTrial(PrototypeFlowCommand command, bool portalEntry)
        {
            if (_entryRoutine == null) _entryRoutine = StartCoroutine(PrepareTrial(command, portalEntry));
        }

        private IEnumerator PrepareTrial(PrototypeFlowCommand command, bool portalEntry)
        {
            if (portalEntry)
            {
                _deck.Show(ThresholdScreen.Playing);
                if (_backdrop.BeginPortalEntry())
                    while (!_backdrop.PortalEntryComplete) yield return null;
            }
            _loading.SetVisible(true);
            // Render the loading cover before the synchronous map and object build blocks the main thread.
            yield return null;
            CommandRequested?.Invoke(command);
            yield return null;
            _loading.SetVisible(false);
            _entryRoutine = null;
        }

        private void CancelPendingEntry()
        {
            if (_entryRoutine != null) StopCoroutine(_entryRoutine);
            _entryRoutine = null;
            if (_loading != null) _loading.SetVisible(false);
        }
        private void CloseTitleSettings()
        {
            if (_titleSettings != null) _titleSettings.Hide();
            if (_backdrop != null) _backdrop.SetModal(false);
        }
        private void OnDestroy() => Unbind();
    }
}
