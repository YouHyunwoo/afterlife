using UnityEngine;
using UnityEngine.Serialization;
using Afterlife.Modules.TargetContracts;

namespace Afterlife.Modules.Interaction
{
    public sealed class PickSurface : MonoBehaviour
    {
        private PickMeshData _overrideMesh;
        private PickMeshCache.Lease _lease;
        private MeshFilter _filter;
        private Mesh _unreadableSource;

        // 직렬화하지 않는다. 변형 지형/눈은 기존처럼 외부 소유 데이터를 주입할 수 있다.
        public PickMeshData Mesh
        {
            get
            {
                if (_overrideMesh != null) return _overrideMesh;
                if (_filter == null) _filter = GetComponent<MeshFilter>();
                var source = _filter != null ? _filter.sharedMesh : null;
                if (_lease != null && (_lease.Source != source || _lease.Data == null))
                { PickMeshCache.Release(_lease, this); _lease = null; }
                if (source == null) return null;
                if (!source.isReadable)
                {
                    if (_unreadableSource != source)
                    { Debug.LogError($"Picking requires Read/Write Enabled: {source.name}", this); _unreadableSource = source; }
                    return null;
                }
                if (_lease == null) _lease = PickMeshCache.Acquire(source, this);
                return _lease.Data;
            }
            set { _overrideMesh = _lease != null && value == _lease.Data ? null : value; }
        }

        private void OnDestroy()
        {
            PickMeshCache.Release(_lease, this);
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

        private PickMeshData _hitMesh;
        private int _hitTriangle;
        private Vector3 _hitWeights;
        private readonly System.Collections.Generic.List<Material> _sampleMaterials=new System.Collections.Generic.List<Material>();
        public Color SampleHitColor(Vector3 point) => SampleColor(_hitMesh,_hitTriangle,_hitWeights,point);

        public bool Intersect(Ray worldRay, out float distance, out Color surfaceColor)
            => Intersect(worldRay,out distance,out surfaceColor,true);

        public bool Intersect(Ray worldRay, out float distance, out Color surfaceColor,bool sampleColor)
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
            _hitMesh=mesh; _hitTriangle=nearestTriangle; _hitWeights=weights;
            if (sampleColor) surfaceColor = SampleHitColor(worldRay.GetPoint(distance));
            return true;
        }

        private Color SampleColor(PickMeshData mesh, int triangle, Vector3 weights,Vector3 worldPoint)
        {
            VisibleRenderer.GetSharedMaterials(_sampleMaterials);
            var materials = _sampleMaterials;
            int slot = mesh.TriangleMaterials != null && triangle/3 < mesh.TriangleMaterials.Length ? mesh.TriangleMaterials[triangle/3] : 0;
            var material = materials.Count == 0 ? null : materials[Mathf.Clamp(slot,0,materials.Count-1)];
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

    // 정적 표면은 공간 트리로 좁히고 움직이는 타깃은 현재 경계로 확인한다.
    // 정적 배치/메시가 변경되면 소유자가 Rebuild를 호출한다. Unity 에셋을 소유하지 않는다.
    public sealed class PickSurfaceIndex
    {
        private sealed class Node
        {
            public Bounds Bounds;
            public Node Left,Right;
            public PickSurface Surface;
        }
        private readonly System.Collections.Generic.List<PickSurface> _moving=new System.Collections.Generic.List<PickSurface>();
        private Node _root;
        public void Clear() { _root=null; _moving.Clear(); }
        private static Bounds Expanded(PickSurface surface)
        {
            var bounds=surface.VisibleRenderer.bounds;
            bounds.Expand(1f); // 바람·호버·해안 변형의 최대 오프셋보다 넉넉한 경계.
            return bounds;
        }
        public void Rebuild(PickSurface[] surfaces)
        {
            _moving.Clear();
            var nodes=new System.Collections.Generic.List<Node>();
            foreach (var surface in surfaces)
            {
                if (surface==null || surface.VisibleRenderer==null) continue;
                if (surface.Target!=null) _moving.Add(surface);
                else nodes.Add(new Node { Bounds=Expanded(surface),Surface=surface });
            }
            _root=Build(nodes.ToArray(),0,nodes.Count);
        }
        private static Node Build(Node[] nodes,int start,int count)
        {
            if (count==0) return null;
            if (count==1) return nodes[start];
            var bounds=nodes[start].Bounds;
            for (int i=start+1;i<start+count;i++) bounds.Encapsulate(nodes[i].Bounds);
            int axis=bounds.size.x>=bounds.size.z ? 0 : 2;
            System.Array.Sort(nodes,start,count,System.Collections.Generic.Comparer<Node>.Create((a,b)=>a.Bounds.center[axis].CompareTo(b.Bounds.center[axis])));
            int half=count/2;
            return new Node { Bounds=bounds,Left=Build(nodes,start,half),Right=Build(nodes,start+half,count-half) };
        }
        public bool Pick(Ray ray,float maximum,out PickSurface nearest,out Vector3 point,out Color color)
        {
            nearest=null;
            Search(_root,ray,ref maximum,ref nearest);
            foreach (var surface in _moving)
                if (surface!=null && surface.isActiveAndEnabled && surface.VisibleRenderer!=null && surface.VisibleRenderer.enabled
                    && (surface.Target==null || !surface.Target.IsBroken) && Expanded(surface).IntersectRay(ray,out float entry) && entry<=maximum)
                    Test(surface,ray,ref maximum,ref nearest);
            point=ray.GetPoint(maximum);
            color=nearest!=null ? nearest.SampleHitColor(point) : Color.white;
            return nearest!=null;
        }
        private static void Search(Node node,Ray ray,ref float closest,ref PickSurface nearest)
        {
            if (node==null || !node.Bounds.IntersectRay(ray,out float entry) || entry>closest) return;
            if (node.Surface!=null) { Test(node.Surface,ray,ref closest,ref nearest); return; }
            Search(node.Left,ray,ref closest,ref nearest);
            Search(node.Right,ray,ref closest,ref nearest);
        }
        private static void Test(PickSurface surface,Ray ray,ref float closest,ref PickSurface nearest)
        {
            if (surface.Intersect(ray,out float distance,out _,false) && distance<closest)
            { closest=distance; nearest=surface; }
        }
    }
}
