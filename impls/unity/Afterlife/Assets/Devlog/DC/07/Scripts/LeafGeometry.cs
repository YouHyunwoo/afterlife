using System.Collections.Generic;
using UnityEngine;

public struct LeafPoint
{
    public Vector3 position;
    public Vector3 normal;
    public Color color;

    public LeafPoint(Vector3 position, Vector3 normal, Color color)
    {
        this.position = position;
        this.normal = normal;
        this.color = color;
    }
}

public class LeafGeometry
{
    public static List<LeafPoint> Calculate(MeshFilter meshFilter, int leafCount = 2000, int seed = 12345)
    {
        Mesh mesh = meshFilter.sharedMesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Color[] colors = mesh.colors;

        Matrix4x4 localToWorld = meshFilter.transform.localToWorldMatrix;

        var random = new System.Random(seed);
        var result = new List<LeafPoint>(leafCount);

        // 삼각형별 누적 면적
        float[] cumulativeAreas = new float[triangles.Length / 3];
        float totalArea = 0f;

        for (int t = 0; t < triangles.Length; t += 3)
        {
            Vector3 a = localToWorld.MultiplyPoint3x4(vertices[triangles[t]]);
            Vector3 b = localToWorld.MultiplyPoint3x4(vertices[triangles[t + 1]]);
            Vector3 c = localToWorld.MultiplyPoint3x4(vertices[triangles[t + 2]]);

            float area = Vector3.Cross(b - a, c - a).magnitude * 0.5f;

            totalArea += area;
            cumulativeAreas[t / 3] = totalArea;
        }

        // 원하는 개수만큼 잎 위치 생성
        for (int i = 0; i < leafCount; i++)
        {
            if (colors==null || colors.Length!=vertices.Length) continue;
            // 1. 면적 비율에 따라 삼각형 선택
            float targetArea = (float)random.NextDouble() * totalArea;

            int triangleIndex = FindTriangle(cumulativeAreas, targetArea);

            int index = triangleIndex * 3;

            Vector3 a = localToWorld.MultiplyPoint3x4(vertices[triangles[index]]);
            Vector3 b = localToWorld.MultiplyPoint3x4(vertices[triangles[index + 1]]);
            Vector3 c = localToWorld.MultiplyPoint3x4(vertices[triangles[index + 2]]);

            // 2. 삼각형 내부의 랜덤 위치
            float r1 = (float)random.NextDouble();
            float r2 = (float)random.NextDouble();

            float sqrtR1 = Mathf.Sqrt(r1);

            float wa = 1f - sqrtR1;
            float wb = sqrtR1 * (1f - r2);
            float wc = sqrtR1 * r2;

            Vector3 position =
                a * wa +
                b * wb +
                c * wc;

            // 3. 삼각형 표면 방향
            Vector3 normal =
                Vector3.Cross(b - a, c - a).normalized;

            Color color = colors[triangles[index]] * wa + colors[triangles[index+1]] * wb + colors[triangles[index+2]] * wc;

            result.Add(new LeafPoint(position, normal, color));
        }

        return result;
    }

    private static int FindTriangle(float[] cumulativeAreas, float target)
    {
        for (int i = 0; i < cumulativeAreas.Length; i++)
        {
            if (target <= cumulativeAreas[i])
                return i;
        }

        return cumulativeAreas.Length - 1;
    }
}
