using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // 런타임 전용 표면 데이터. 원본 메시별 공유 수명은 ShowcasePickMeshCache가 관리한다.
    public sealed class ShowcasePickMesh : ScriptableObject
    {
        public Vector3[] Vertices;
        public Vector3[] Normals;
        public int[] Triangles;
        public Bounds Bounds;
        public Color[] Colors;
        public int[] TriangleMaterials;
        public int ColorVersion;

        public static ShowcasePickMesh Create(Mesh source)
        {
            if (source == null) throw new System.ArgumentNullException(nameof(source));
            if (!source.isReadable)
                throw new System.InvalidOperationException($"Picking requires Read/Write Enabled: {source.name}");
            var data = CreateInstance<ShowcasePickMesh>();
            data.hideFlags = HideFlags.DontSave;
            try
            {
                data.name = source.name + "_RuntimePick";
                data.Vertices = source.vertices;
                data.Normals = source.normals;
                data.Triangles = source.triangles;
                data.Colors = source.colors;
                data.Bounds = source.bounds;
                data.Bounds.Expand(.06f);
                var slots = new System.Collections.Generic.List<int>(data.Triangles.Length / 3);
                for (int sub = 0; sub < source.subMeshCount; sub++)
                    for (int i = 0, count = (int)source.GetIndexCount(sub) / 3; i < count; i++) slots.Add(sub);
                data.TriangleMaterials = slots.ToArray();
                data.ColorVersion = 2;
                return data;
            }
            catch
            {
                if (Application.isPlaying) Destroy(data); else DestroyImmediate(data);
                throw;
            }
        }
    }
}
