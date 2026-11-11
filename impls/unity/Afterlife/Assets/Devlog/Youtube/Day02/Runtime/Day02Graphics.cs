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
        private readonly Image _background;
        public Day02Graphics(Transform parent, TMP_FontAsset font)
        {
            _root = new GameObject("Day02Graphics", typeof(Canvas), typeof(CanvasScaler));
            _root.transform.SetParent(parent, false);
            var canvas = _root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 1500;
            var scaler = _root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920,1080);
            _panel = new GameObject("Explanation", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(_root.transform,false);
            Rect(_panel,new Vector2(.10f,.72f),new Vector2(.90f,.82f));
            _background = _panel.GetComponent<Image>(); _background.color = new Color(.035f,.06f,.085f,.94f); _background.raycastTarget = false;
            _diagram = Label("Diagram",_panel.transform,font,32,new Vector2(.04f,.06f),new Vector2(.96f,.94f));
        }
        public void Show(FilmShot shot,Day02ShotPresentation direction)
        {
            string explanation=direction!=null?direction.Explanation:null;
            bool teaser=direction!=null&&direction.Teaser;
            _panel.SetActive(!shot.ShowCredits&&!string.IsNullOrWhiteSpace(explanation));
            // Place the teaser caption above the target, with an explicit input/result distinction.
            Rect(_panel,teaser?new Vector2(.18f,.70f):new Vector2(.10f,.72f),teaser?new Vector2(.82f,.86f):new Vector2(.90f,.82f));
            _diagram.fontSize=teaser?38:32;
            _background.color=!teaser?new Color(.035f,.06f,.085f,.94f):
                shot.Stage==0?new Color(.09f,.15f,.23f,.95f):
                shot.Stage==1?new Color(.05f,.23f,.19f,.95f):new Color(.25f,.18f,.04f,.95f);
            _diagram.text=teaser?"<b>"+shot.Title+"</b>\n<size=28>"+explanation+"</size>":explanation??"";
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
