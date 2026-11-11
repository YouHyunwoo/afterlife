using System.Collections.Generic;
using UnityEngine;

public class LeafRenderer : MonoBehaviour
{
    [SerializeField] private Transform leavesContainer;
    [SerializeField] private MeshFilter canopy;
    [SerializeField] private float leafSize;
    [SerializeField] private Material leafMaterial;
    [SerializeField] private int seed = 12345;

    private Mesh generatedMesh;

    void Start()
    {
        var leafPoints = LeafGeometry.Calculate(canopy);

        CreateLeafMesh(leafPoints);

        // foreach (var leafPoint in leafPoints)
        // {
        //     GameObject leaf = GameObject.CreatePrimitive(PrimitiveType.Quad);
        //     leaf.transform.localScale = Vector3.one * leafSize;
        //     leaf.transform.position = leafPoint.position;
        //     leaf.transform.rotation = Quaternion.LookRotation(-leafPoint.normal);
        //     leaf.transform.SetParent(leavesContainer, true);
        // }
    }

    private void CreateLeafMesh(List<LeafPoint> points)
    {
        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var colors = new List<Color>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();

        System.Random random = new System.Random(seed);

        Transform canopyTransform = canopy.transform;

        foreach (LeafPoint point in points)
        {
            // LeafPointGenerator가 World 좌표를 반환하므로
            // 다시 수관 Local 좌표로 변환
            Vector3 center = point.position;
                // canopyTransform.InverseTransformPoint(
                //     point.position
                // );

            Vector3 normal = point.normal;
                // canopyTransform.InverseTransformDirection(
                //     point.normal
                // ).normalized;

            Color color = point.color;

            // 크기 약간 랜덤
            float size =
                leafSize *
                Mathf.Lerp(
                    0.8f,
                    1.2f,
                    (float)random.NextDouble()
                );

            // float half = size * 0.5f;
            float width = size;
            float height = size * 0.8f;

            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            // 표면 Normal 방향을 바라보게 함
            Quaternion rotation =
                Quaternion.LookRotation(normal);

            // Normal 축 기준 랜덤 회전
            // float angle =
            //     (float)random.NextDouble() * 360f;

            // rotation *=
            //     Quaternion.AngleAxis(
            //         angle,
            //         Vector3.forward
            //     );

            // Quad의 네 모서리
            // Vector3 v0 =
            //     center +
            //     rotation *
            //     new Vector3(-half, -half, 0);

            // Vector3 v1 =
            //     center +
            //     rotation *
            //     new Vector3(half, -half, 0);

            // Vector3 v2 =
            //     center +
            //     rotation *
            //     new Vector3(half, half, 0);

            // Vector3 v3 =
            //     center +
            //     rotation *
            //     new Vector3(-half, half, 0);

            Vector3 v0 = center + rotation * new Vector3(-halfWidth, -halfHeight, 0);
            Vector3 v1 = center + rotation * new Vector3( halfWidth, -halfHeight, 0);
            Vector3 v2 = center + rotation * new Vector3( halfWidth,  halfHeight, 0);
            Vector3 v3 = center + rotation * new Vector3(-halfWidth,  halfHeight, 0);

            int start = vertices.Count;

            vertices.Add(v0);
            vertices.Add(v1);
            vertices.Add(v2);
            vertices.Add(v3);

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            colors.Add(color);
            colors.Add(color);
            colors.Add(color);
            colors.Add(color);

            uvs.Add(new Vector2(0, 0));
            uvs.Add(new Vector2(1, 0));
            uvs.Add(new Vector2(1, 1));
            uvs.Add(new Vector2(0, 1));

            // Quad = Triangle 2개
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);

            triangles.Add(start);
            triangles.Add(start + 2);
            triangles.Add(start + 3);
        }

        generatedMesh = new Mesh();
        generatedMesh.name = "GeneratedLeafMesh";

        // 잎이 아주 많아졌을 때를 대비
        generatedMesh.indexFormat =
            UnityEngine.Rendering.IndexFormat.UInt32;

        generatedMesh.SetVertices(vertices);
        generatedMesh.SetNormals(normals);
        generatedMesh.SetColors(colors);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(triangles, 0);

        generatedMesh.RecalculateBounds();

        // 실제 렌더링용 GameObject는 딱 하나
        GameObject leafObject =
            new GameObject("LeafCards");

        leafObject.transform.SetParent(
            leavesContainer,
            true
        );

        MeshFilter filter =
            leafObject.AddComponent<MeshFilter>();

        MeshRenderer renderer =
            leafObject.AddComponent<MeshRenderer>();

        filter.sharedMesh = generatedMesh;

        if (leafMaterial != null)
            renderer.sharedMaterial = leafMaterial;
    }
}