using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // ShowcaseSnowErosion.hlsl과 정수 해시/보간식을 공유한다. 경계가 시간에 따라 반짝이지 않는다.
    public static class ShowcaseSnowErosion
    {
        private static float Hash(int x,int y,int z)
        {
            unchecked
            {
                uint h=(uint)x*73856093u ^ (uint)y*19349663u ^ (uint)z*83492791u;
                h=(h^(h>>13))*1274126177u;
                return (h&65535u)/65535f;
            }
        }
        public static float Noise(Vector3 p)
        {
            int x=Mathf.FloorToInt(p.x), y=Mathf.FloorToInt(p.y), z=Mathf.FloorToInt(p.z);
            float fx=p.x-x,fy=p.y-y,fz=p.z-z;
            fx=fx*fx*(3-2*fx); fy=fy*fy*(3-2*fy); fz=fz*fz*(3-2*fz);
            return Mathf.Lerp(Mathf.Lerp(Mathf.Lerp(Hash(x,y,z),Hash(x+1,y,z),fx),Mathf.Lerp(Hash(x,y+1,z),Hash(x+1,y+1,z),fx),fy),
                Mathf.Lerp(Mathf.Lerp(Hash(x,y,z+1),Hash(x+1,y,z+1),fx),Mathf.Lerp(Hash(x,y+1,z+1),Hash(x+1,y+1,z+1),fx),fy),fz);
        }
        public static float Influence(Vector3 local,Vector3 hit)
        {
            Vector3 delta=Vector3.Scale(local-hit,new Vector3(1,.88f,1.08f));
            float distance=delta.magnitude+(Noise(local*8+hit*3)-.5f)*.16f+(Noise(local*23)-.5f)*.045f;
            return 1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.08f,.44f,distance));
        }
        public static bool Visible(Vector3 local,float remaining)
        {
            float coverage=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.025f,.40f,remaining));
            return coverage >= .015f+.97f*Hash(Mathf.FloorToInt(local.x*95),Mathf.FloorToInt(local.y*95),Mathf.FloorToInt(local.z*95));
        }
    }
}
