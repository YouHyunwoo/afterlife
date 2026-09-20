using System;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Prototype
{
    // 기본 화면 표현. 게임 흐름 규칙 없이 사용자 의도만 전달한다.
    // 향후 아트/프리팹 화면은 이 클래스의 Show와 Requested 계약을 유지해 교체한다.
    public sealed class PrototypeFlowScreen : IDisposable
    {
        private readonly GameObject _root;
        private readonly Text _title, _body, _actionLabel;
        private readonly Button _action;
        private GameScreen _screen;
        private int _shownFrame;
        public event Action<GameScreen> Requested;

        public PrototypeFlowScreen(Transform parent, Font font)
        {
            _root = new GameObject("Game Flow Screens", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20000;
            var scaler = _root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200, 675);
            scaler.matchWidthOrHeight = .5f;
            var background = Element("Background", _root.transform, Vector2.zero, Vector2.one, Vector2.zero);
            background.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .07f, 1);
            _title = Label("Title", font, new Vector2(.08f, .63f), new Vector2(.92f, .82f), 42, new Color(1, .78f, .25f));
            _body = Label("Body", font, new Vector2(.12f, .32f), new Vector2(.88f, .61f), 24, new Color(.93f, .88f, .72f));
            var buttonRect = Element("Continue", _root.transform, new Vector2(.5f, .21f), new Vector2(.5f, .21f), new Vector2(300, 64));
            var image = buttonRect.gameObject.AddComponent<Image>(); image.color = new Color(.13f, .31f, .29f);
            _action = buttonRect.gameObject.AddComponent<Button>(); _action.targetGraphic = image;
            var colors = _action.colors; colors.highlightedColor = new Color(.65f, 1, .9f); colors.pressedColor = new Color(.35f, .8f, .7f); _action.colors = colors;
            var outline = buttonRect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(1, .78f, .25f); outline.effectDistance = new Vector2(1, -1);
            var textRect = Element("Label", buttonRect, Vector2.zero, Vector2.one, Vector2.zero);
            _actionLabel = textRect.gameObject.AddComponent<Text>(); Configure(_actionLabel, font, 24, Color.white);
            _action.onClick.AddListener(OnRequested);
            _root.SetActive(false);
        }

        private Text Label(string name, Font font, Vector2 min, Vector2 max, int size, Color color)
        {
            var rect = Element(name, _root.transform, min, max, Vector2.zero);
            var label = rect.gameObject.AddComponent<Text>(); Configure(label, font, size, color); return label;
        }
        private static void Configure(Text text, Font font, int size, Color color)
        { text.font = font; text.fontSize = size; text.color = color; text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false; }
        private static RectTransform Element(string name, Transform parent, Vector2 min, Vector2 max, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = min; rect.anchorMax = max; rect.sizeDelta = size; rect.anchoredPosition = Vector2.zero; return rect;
        }
        private void OnRequested()
        {
            // 같은 프레임의 중복 클릭이 다음 화면까지 통과하지 않게 한다.
            if (Time.frameCount <= _shownFrame) return;
            Requested?.Invoke(_screen);
        }
        public void Show(GameScreen screen, GameFlowResult result)
        {
            _screen = screen; _shownFrame = Time.frameCount;
            _root.SetActive(screen != GameScreen.Playing);
            if (screen == GameScreen.Playing) return;
            _title.text = screen == GameScreen.Title ? "AFTERLIFE" : screen == GameScreen.Intro ? "마지막 불빛을 지켜주세요" : screen == GameScreen.Clear ? "마을에 찾아온 평화" : "마지막 불빛이 꺼졌습니다";
            _body.text = screen == GameScreen.Title ? "작은 집 하나에서 시작하는 마을의 이야기" : screen == GameScreen.Intro
                ? "낮에는 자원을 모으고 마을을 키우세요.\n밤에는 다가오는 적으로부터 집을 지키세요.\n마지막 밤까지 살아남으면 마을에 평화가 찾아옵니다."
                : $"{(screen == GameScreen.Clear ? "클리어" : "게임 오버")}\n최고 {result?.BestCleared ?? 0}일 방어 · 환생 {result?.Rebirths ?? 0}회 · 처치 {result?.Kills ?? 0}\n영구 해금은 다음 판에도 유지됩니다.";
            _actionLabel.text = screen == GameScreen.Title ? "게임 시작" : screen == GameScreen.Intro ? "마을로 들어가기" : "타이틀로";
        }
        public void Dispose()
        {
            _action.onClick.RemoveListener(OnRequested); Requested = null;
            if (_root != null) UnityEngine.Object.Destroy(_root);
        }
    }
}
