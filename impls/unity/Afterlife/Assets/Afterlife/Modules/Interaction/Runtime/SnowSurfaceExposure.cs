using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Modules.Interaction
{
    // 생성 시의 위쪽 노출도를 저장한다. 쓰러짐 회전 후에는 다시 계산하지 않는다.
    public sealed class SnowSurfaceExposure
    {
        private readonly List<Vector3> _triangles=new List<Vector3>();
        public void Add(Transform transform,Vector3[] vertices,int[] triangles)
        {
            foreach (int index in triangles) _triangles.Add(transform.TransformPoint(vertices[index]));
        }
        public float At(Vector3 point,Vector3 normal)
        {
            float exposure=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.05f,.45f,normal.y));
            if (exposure<=0) return 0;
            for (int i=0;i<_triangles.Count;i+=3)
            {
                Vector3 a=_triangles[i],b=_triangles[i+1]-a,c=_triangles[i+2]-a,d=point-a;
                float det=b.x*c.z-b.z*c.x;
                if (Mathf.Abs(det)<.000001f) continue;
                float u=(d.x*c.z-d.z*c.x)/det,v=(b.x*d.z-b.z*d.x)/det;
                if (u<0 || v<0 || u+v>1) continue;
                if (a.y+b.y*u+c.y*v>point.y+.025f) return 0;
            }
            return exposure;
        }
    }
}
