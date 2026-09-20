using UnityEngine;
using UnityEngine.Serialization;
using Afterlife.Devlog.Contracts;

namespace Afterlife.Devlog.Day01
{
    public sealed class ShowcasePickSurface : MonoBehaviour
    {
        private ShowcasePickMesh _overrideMesh;
        private ShowcasePickMeshCache.Lease _lease;
        private MeshFilter _filter;
        private Mesh _unreadableSource;

        // 직렬화하지 않는다. 변형 지형/눈은 기존처럼 외부 소유 데이터를 주입할 수 있다.
        public ShowcasePickMesh Mesh
        {
            get
            {
                if (_overrideMesh != null) return _overrideMesh;
                if (_filter == null) _filter = GetComponent<MeshFilter>();
                var source = _filter != null ? _filter.sharedMesh : null;
                if (_lease != null && (_lease.Source != source || _lease.Data == null))
                { ShowcasePickMeshCache.Release(_lease, this); _lease = null; }
                if (source == null) return null;
                if (!source.isReadable)
                {
                    if (_unreadableSource != source)
                    { Debug.LogError($"Picking requires Read/Write Enabled: {source.name}", this); _unreadableSource = source; }
                    return null;
                }
                if (_lease == null) _lease = ShowcasePickMeshCache.Acquire(source, this);
                return _lease.Data;
            }
            set { _overrideMesh = _lease != null && value == _lease.Data ? null : value; }
        }

        private void OnDestroy()
        {
            ShowcasePickMeshCache.Release(_lease, this);
            _lease = null;
            _overrideMesh = null; // 외부 주입 데이터는 주입한 모듈이 해제한다.
        }
        public Renderer VisibleRenderer;
        [FormerlySerializedAs("Target")] [SerializeField] private MonoBehaviour _target;
        public IPickTarget Target { get => _target as IPickTarget; set => _target=value as MonoBehaviour; }
        public bool SnowCap;
        public Vector3 HitNormal { get; private set; }
        public System.Func<Vector3,float> HeightOffset;
        private Vector3[] _deformed;

        public bool Intersect(Ray worldRay, out float distance, out Color surfaceColor)
        {
            distance = float.PositiveInfinity;
            surfaceColor = Color.white;
            if (Target!=null && Target.IsBroken) return false;
            if (!isActiveAndEnabled || VisibleRenderer == null || !VisibleRenderer.enabled) return false;
            var mesh = Mesh;
            if (mesh == null) return false;
            // 방향을 정규화하지 않아 t가 월드 거리로 유지된다. 매 프레임 실제 모션을 반영한다.
            Vector3 origin = transform.InverseTransformPoint(worldRay.origin);
            Vector3 direction = transform.InverseTransformVector(worldRay.direction);
            Bounds bounds = mesh.Bounds;
            bounds.Expand(transform.InverseTransformVector(Vector3.up*.14f).magnitude*2);
            if (!bounds.IntersectRay(new Ray(origin, direction))) return false;
            var vertices = mesh.Vertices;
            var material = VisibleRenderer.sharedMaterial;
            float wind = material != null && material.HasProperty("_Wind") ? material.GetFloat("_Wind") : 0;
            if (wind != 0 || HeightOffset!=null)
            {
                if (_deformed == null || _deformed.Length != vertices.Length) _deformed = new Vector3[vertices.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    Vector3 world = transform.TransformPoint(vertices[i]);
                    world.x += Mathf.Sin(Time.time*1.6f+world.z*1.7f+world.x)*.025f*wind*Mathf.Clamp01(world.y*2);
                    if (HeightOffset!=null) world.y+=HeightOffset(world);
                    _deformed[i] = transform.InverseTransformPoint(world);
                }
                vertices = _deformed;
            }
            var triangles = mesh.Triangles;
            int nearestTriangle = -1;
            Vector3 weights = Vector3.zero;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]], ab = vertices[triangles[i+1]]-a, ac = vertices[triangles[i+2]]-a;
                Vector3 p = Vector3.Cross(direction, ac);
                float det = Vector3.Dot(ab, p);
                if (Mathf.Abs(det) < 1e-9f) continue;
                float inverse = 1 / det;
                Vector3 delta = origin-a;
                float u = Vector3.Dot(delta,p)*inverse;
                if (u < 0 || u > 1) continue;
                Vector3 q = Vector3.Cross(delta,ab);
                float v = Vector3.Dot(direction,q)*inverse;
                if (v < 0 || u+v > 1) continue;
                float t = Vector3.Dot(ac,q)*inverse;
                if (SnowCap && Target != null)
                {
                    // 셰이더가 바람을 적용하기 전 위치에서 계산한 눈 마스크와 일치시킨다.
                    Vector3 maskPoint=transform.TransformPoint(mesh.Vertices[triangles[i]]*(1-u-v)
                        +mesh.Vertices[triangles[i+1]]*u+mesh.Vertices[triangles[i+2]]*v);
                    if (!Target.SnowVisible(maskPoint)) continue;
                }
                if (t > 0 && t < distance)
                { distance = t; nearestTriangle = i; weights = new Vector3(1-u-v,u,v); }
            }
            if (nearestTriangle < 0) return false;
            Vector3 normal=Vector3.Cross(vertices[triangles[nearestTriangle+1]]-vertices[triangles[nearestTriangle]],
                vertices[triangles[nearestTriangle+2]]-vertices[triangles[nearestTriangle]]).normalized;
            HitNormal=transform.worldToLocalMatrix.transpose.MultiplyVector(normal).normalized;
            if (Vector3.Dot(HitNormal,worldRay.direction)>0) HitNormal=-HitNormal;
            surfaceColor = SampleColor(mesh, nearestTriangle, weights,worldRay.GetPoint(distance));
            return true;
        }

        private Color SampleColor(ShowcasePickMesh mesh, int triangle, Vector3 weights,Vector3 worldPoint)
        {
            var materials = VisibleRenderer.sharedMaterials;
            int slot = mesh.TriangleMaterials != null && triangle/3 < mesh.TriangleMaterials.Length ? mesh.TriangleMaterials[triangle/3] : 0;
            var material = materials.Length == 0 ? null : materials[Mathf.Clamp(slot,0,materials.Length-1)];
            Color tint = material != null && material.HasProperty("_BaseColor") ? material.GetColor("_BaseColor")
                : material != null && material.HasProperty("_ShallowColor") ? material.GetColor("_ShallowColor") : Color.white;
            bool linear = QualitySettings.activeColorSpace == ColorSpace.Linear;
            if (material!=null && material.HasProperty("_TerrainBlend") && material.GetFloat("_TerrainBlend")>.5f
                && material.GetTexture("_TerrainColorMap") is Texture2D map && map.isReadable)
            {
                Vector4 area=material.GetVector("_TerrainMapBounds");
                Color blended=map.GetPixel(Mathf.Clamp(Mathf.FloorToInt((worldPoint.x-area.x)/area.z*map.width),0,map.width-1),
                    Mathf.Clamp(Mathf.FloorToInt((worldPoint.z-area.y)/area.w*map.height),0,map.height-1));
                return linear ? blended.linear : blended;
            }
            if (linear) tint = tint.linear;
            Color palette = Color.white;
            if (mesh.Colors != null && mesh.Colors.Length == mesh.Vertices.Length)
            {
                Color a = mesh.Colors[mesh.Triangles[triangle]];
                Color b = mesh.Colors[mesh.Triangles[triangle+1]];
                Color c = mesh.Colors[mesh.Triangles[triangle+2]];
                // ShowcaseSurface와 동일하게 정점별 색 공간 변환 후 보간한다.
                if (linear) { a = a.linear; b = b.linear; c = c.linear; }
                palette = a*weights.x + b*weights.y + c*weights.z;
            }
            float usePalette = material != null && material.HasProperty("_VertexPalette") ? material.GetFloat("_VertexPalette") : 0;
            Color color = tint * Color.Lerp(Color.white,palette,Mathf.Clamp01(usePalette));
            color.a = 1;
            return color;
        }
    }
}
