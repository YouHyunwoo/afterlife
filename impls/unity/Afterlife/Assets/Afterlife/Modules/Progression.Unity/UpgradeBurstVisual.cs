using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Progression
{
    // 고정 크기 풀: 연속 강화에도 GameObject/Material을 생성하지 않는다.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class UpgradeBurstVisual : MaskableGraphic
    {
        private struct Particle { public Vector2 Origin, Direction; public float Born, Radius, Distance, Angle; public bool Crystal, Active; }
        private readonly Particle[] _particles = new Particle[128];
        private int _cursor;
        protected override void Awake() { base.Awake(); raycastTarget = false; }
        public void Play(Vector2 origin, bool laboratory)
        {
            for (int i = 0; i < 16; i++)
            {
                float angle = (i + Random.value * .6f) * Mathf.PI * 2 / 16;
                _particles[_cursor] = new Particle { Origin = origin, Direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)),
                    Born = Time.unscaledTime, Radius = Random.Range(3f, 7f) * (laboratory ? 1 : 1.3f), Distance = Random.Range(64f, 115f),
                    Angle = Random.Range(0f, 360f), Crystal = !laboratory, Active = true };
                _cursor = (_cursor + 1) % _particles.Length;
            }
            SetVerticesDirty();
        }
        private void Update()
        {
            bool dirty = false;
            for (int i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].Active) continue;
                dirty = true;
                if (Time.unscaledTime - _particles[i].Born >= .8f) _particles[i].Active = false;
            }
            if (dirty) SetVerticesDirty();
        }
        protected override void OnDisable()
        {
            System.Array.Clear(_particles, 0, _particles.Length);
            base.OnDisable();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            foreach (var particle in _particles)
            {
                if (!particle.Active) continue;
                float t = Mathf.Clamp01((Time.unscaledTime - particle.Born) / .8f);
                Vector2 position = particle.Origin + particle.Direction * (particle.Distance * (1 - Mathf.Pow(1 - t, 3)));
                float alpha = Mathf.Pow(1 - t, 1.5f) * .8f;
                float radius = particle.Radius * (1 - .35f * t);
                if (particle.Crystal)
                {
                    Quaternion rotation = Quaternion.Euler(0, 0, particle.Angle + t * 100);
                    Vector2 top = position + (Vector2)(rotation * new Vector3(0, radius * 1.5f));
                    Vector2 right = position + (Vector2)(rotation * new Vector3(radius * .65f, 0));
                    Vector2 bottom = position + (Vector2)(rotation * new Vector3(0, -radius * 1.5f));
                    Vector2 left = position + (Vector2)(rotation * new Vector3(-radius * .65f, 0));
                    Triangle(mesh, top, right, bottom, new Color(.48f, .84f, .77f, alpha));
                    Triangle(mesh, top, bottom, left, new Color(.47f, .4f, .61f, alpha));
                }
                else
                {
                    for (int segment = 0; segment < 16; segment++)
                    {
                        float a = segment * Mathf.PI / 8, b = (segment + 1) * Mathf.PI / 8;
                        Vector2 first = new Vector2(Mathf.Cos(a), Mathf.Sin(a)), second = new Vector2(Mathf.Cos(b), Mathf.Sin(b));
                        Color tint = new Color(.48f, .84f, .77f, alpha);
                        Triangle(mesh, position + first * radius, position + second * radius, position + second * (radius * .7f), tint);
                        Triangle(mesh, position + first * radius, position + second * (radius * .7f), position + first * (radius * .7f), tint);
                    }
                }
            }
        }
        private static void Triangle(VertexHelper mesh, Vector2 a, Vector2 b, Vector2 c, Color color)
        {
            int start = mesh.currentVertCount;
            mesh.AddVert(a, color, Vector2.zero); mesh.AddVert(b, color, Vector2.zero); mesh.AddVert(c, color, Vector2.zero);
            mesh.AddTriangle(start, start + 1, start + 2);
        }
    }
}
