using Afterlife.Devlog.Cinematics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Devlog.Day02
{
    internal sealed class Day02Graphics : System.IDisposable
    {
        private readonly GameObject _root;
        private readonly GameObject _panel;
        private readonly TextMeshProUGUI _diagram;
        public Day02Graphics(Transform parent, TMP_FontAsset font)
        {
            _root = new GameObject("Day02Graphics", typeof(Canvas), typeof(CanvasScaler));
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1500;
            var scaler = _root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            _panel = new GameObject("Explanation", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_root.transform,false);
            Rect(_panel,new Vector2(.12f,.32f),new Vector2(.88f,.72f));
            var image = _panel.GetComponent<Image>(); image.color = new Color(.035f,.06f,.085f,.94f); image.raycastTarget = false;
            _diagram = Label("Diagram",_panel.transform,font,42,new Vector2(.04f,.06f),new Vector2(.96f,.94f));
        }
        public void Show(FilmShot shot)
        {
            _panel.SetActive(!shot.AllowInteraction);
            switch (shot.Stage)
            {
                case 0: _diagram.text = "<size=30>게임의 큰 흐름</size>\n\n채집 → 골드 → 건설 → 밤 방어\n\n<size=26>오늘은 채집 시스템을 다듬습니다</size>"; break;
                case 1: _diagram.text = "반복 클릭을 줄여 볼까요?\n\n<size=30>클릭 → 커서를 올려두면 자동 채집</size>"; break;
                case 2: _diagram.text = "커서가 한 곳에 묶여 있다면?\n\n<size=30>영력 필드를 놓고 다른 곳을 살펴보기</size>"; break;
                case 3: _diagram.text = "더 큰 섬에서 자유롭게 채집하기\n\n<size=30>채집 → 골드 → 다음에는 마을 건설</size>"; break;
            }
        }
        private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, float size, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI)); go.transform.SetParent(parent,false); Rect(go,min,max);
            var text = go.GetComponent<TextMeshProUGUI>(); text.font = font; text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private static void Rect(GameObject go, Vector2 min, Vector2 max)
        { var rect = (RectTransform)go.transform; rect.anchorMin=min; rect.anchorMax=max; rect.offsetMin=rect.offsetMax=Vector2.zero; }
        public void Dispose() { if (_root != null) { _root.SetActive(false); Object.Destroy(_root); } }
    }
}
