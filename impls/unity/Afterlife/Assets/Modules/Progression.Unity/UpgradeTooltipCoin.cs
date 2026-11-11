using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeTooltipCoin : MaskableGraphic
    {
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Disc(mesh, 1, new Color32(191, 145, 69, 255));
            Disc(mesh, .72f, new Color32(237, 204, 121, 255));
        }
        private void Disc(VertexHelper mesh, float scale, Color tint)
        {
            int start = mesh.currentVertCount;
            var rect = rectTransform.rect;
            mesh.AddVert(rect.center, tint, Vector2.zero);
            for (int i = 0; i < 8; i++)
            {
                float angle = (22.5f - i * 45) * Mathf.Deg2Rad;
                mesh.AddVert(rect.center + new Vector2(Mathf.Cos(angle) * rect.width, Mathf.Sin(angle) * rect.height) * (.5f * scale), tint, Vector2.zero);
            }
            for (int i = 0; i < 8; i++) mesh.AddTriangle(start, start + i + 1, start + (i + 1) % 8 + 1);
        }
    }
}
