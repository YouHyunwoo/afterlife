using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    // 색상에만 의존하지 않는다: 빈/채운 단계 칸, 완료 이중 테두리와 체크.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeNodeVisual : MaskableGraphic
    {
        public Image Icon;
        public bool BadgeOnly;
        public UpgradeNodeVisual Badge;
        private int _level = -1, _maximum;
        private bool _locked;
        public void SetState(int level, int maximum, bool locked)
        {
            if (_level == level && _maximum == maximum && _locked == locked) return;
            _level = level; _maximum = maximum; _locked = locked;
            if (Badge != null) Badge.SetState(level, maximum, locked);
            if (Icon != null) Icon.color = locked ? new Color(.46f, .5f, .56f, 1) : level == 0 ? new Color(.72f, .76f, .8f, 1) : Color.white;
            SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var rect = rectTransform.rect;
            bool complete = _level >= _maximum && _maximum > 0;
            Color accent = complete ? new Color(.93f, .8f, .47f) : _level > 0 ? new Color(.48f, .84f, .77f) : new Color(.36f, .43f, .53f);
            if (!BadgeOnly)
            {
            Quad(mesh, rect, new Color(.055f, .085f, .13f, 1));
            Frame(mesh, rect, _level > 0 ? 3 : 1.5f, accent);
            if (complete) Frame(mesh, new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), 1, accent);
            int count = Mathf.Max(1, _maximum); float gap = 3, width = Mathf.Min(11, (rect.width - 24 - gap * (count - 1)) / count);
            float start = -(width * count + gap * (count - 1)) / 2;
            for (int index = 0; index < count; index++)
            {
                var pip = new Rect(start + index * (width + gap), rect.yMin + 9, width, 5);
                if (index < _level) Quad(mesh, pip, accent); else Frame(mesh, pip, 1, accent);
            }
            return;
            }
            if (complete)
            {
                var corner = new Vector2(rect.xMax - 17, rect.yMax - 17);
                Quad(mesh, new Rect(corner.x - 3, corner.y - 3, 19, 17), new Color(.055f, .085f, .13f));
                for (int index = 0; index < 3; index++) Quad(mesh, new Rect(corner.x + index * 2, corner.y + 5 - index * 2, 3, 3), accent);
                for (int index = 0; index < 5; index++) Quad(mesh, new Rect(corner.x + 4 + index * 2, corner.y + 1 + index * 2, 3, 3), accent);
            }
            else if (_locked)
            {
                var corner = new Vector2(rect.xMax - 17, rect.yMax - 17);
                Quad(mesh, new Rect(corner.x - 2, corner.y - 2, 16, 17), new Color(.055f, .085f, .13f));
                Quad(mesh, new Rect(corner.x, corner.y, 12, 8), accent);
                Frame(mesh, new Rect(corner.x + 2, corner.y + 6, 8, 7), 2, accent);
            }
        }
        private static void Frame(VertexHelper mesh, Rect rect, float width, Color tint)
        {
            Quad(mesh, new Rect(rect.x, rect.y, rect.width, width), tint); Quad(mesh, new Rect(rect.x, rect.yMax - width, rect.width, width), tint);
            Quad(mesh, new Rect(rect.x, rect.y, width, rect.height), tint); Quad(mesh, new Rect(rect.xMax - width, rect.y, width, rect.height), tint);
        }
        private static void Quad(VertexHelper mesh, Rect rect, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(new Vector2(rect.xMin, rect.yMin), tint, Vector2.zero); mesh.AddVert(new Vector2(rect.xMin, rect.yMax), tint, Vector2.zero);
            mesh.AddVert(new Vector2(rect.xMax, rect.yMax), tint, Vector2.zero); mesh.AddVert(new Vector2(rect.xMax, rect.yMin), tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2); mesh.AddTriangle(start, start + 2, start + 3);
        }
    }
}
