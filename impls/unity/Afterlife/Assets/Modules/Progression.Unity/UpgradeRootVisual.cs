using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    // 구매 노드와 분리된, 트리 중심만 표시하는 장식 그래픽.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeRootVisual : MaskableGraphic
    {
        public bool Laboratory;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
        }

        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Rect rect = rectTransform.rect;
            Vector2 center = rect.center;
            if (Laboratory)
            {
                const int segments = 48;
                float radius = Mathf.Min(rect.width, rect.height) * .5f;
                Color tint = new Color32(122, 214, 196, 255);
                mesh.AddVert(center, tint, Vector2.zero);
                for (int i = 0; i < segments; i++)
                {
                    float angle = -i * Mathf.PI * 2 / segments;
                    mesh.AddVert(center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, tint, Vector2.zero);
                }
                for (int i = 0; i < segments; i++) mesh.AddTriangle(0, i + 1, (i + 1) % segments + 1);
                return;
            }

            Vector2 top = center + new Vector2(0, rect.height * .5f);
            Vector2 left = center + new Vector2(-rect.width * .5f, 0);
            Vector2 right = center + new Vector2(rect.width * .5f, 0);
            Vector2 bottom = center - new Vector2(0, rect.height * .5f);
            Color diamondTint = new Color32(122, 214, 196, 255);
            Face(mesh, top, right, bottom, diamondTint);
            Face(mesh, top, bottom, left, diamondTint);
        }

        private static void Face(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color tint)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, tint, Vector2.zero);
            mesh.AddVert(b, tint, Vector2.zero);
            mesh.AddVert(c, tint, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
