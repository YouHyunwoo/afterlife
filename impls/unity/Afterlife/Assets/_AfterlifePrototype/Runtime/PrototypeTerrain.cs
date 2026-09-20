using System;

namespace Afterlife.Prototype
{
    public enum TerrainKind : byte { Grass, Dirt, Sand, Water, Snow, Forest, Rocky }

    // 1×1 건설 격자와 같은 좌표계. 표현과 생성/이동 규칙이 이 배열 하나를 공유한다.
    public sealed class TerrainMap
    {
        private readonly TerrainKind[] _tiles;
        public int Radius { get; }
        public int Size => Radius*2+1;
        public TerrainMap(int radius)
        {
            Radius=radius;_tiles=new TerrainKind[Size*Size];
            for(int z=-Radius;z<=Radius;z++)for(int x=-Radius;x<=Radius;x++)
            {
                double noise=(Math.Sin(x*.23+Math.Sin(z*.19)*2)+Math.Sin(z*.31+x*.07))*.5;
                double coast=x-Radius*.60+noise*2;
                TerrainKind kind=coast>=0?TerrainKind.Water:coast>=-2?TerrainKind.Sand:z+noise*3>Radius*.55?TerrainKind.Snow:noise>.40?TerrainKind.Dirt:TerrainKind.Grass;
                if(kind!=TerrainKind.Water&&kind!=TerrainKind.Sand&&kind!=TerrainKind.Snow)
                {
                    if(Patch(x,z,-12,-9,5,noise)||Patch(x,z,-15,11,5,noise)||Patch(x,z,7,12,4,noise)||Patch(x,z,4,-18,4,noise))kind=TerrainKind.Forest;
                    if(Patch(x,z,-10,-21,3.5,noise)||Patch(x,z,10,-6,3.5,noise)||Patch(x,z,-23,0,3,noise))kind=TerrainKind.Rocky;
                }
                if(Math.Abs(x)<=5&&Math.Abs(z)<=5)kind=TerrainKind.Grass;
                _tiles[(z+Radius)*Size+x+Radius]=kind;
            }
        }
        private static bool Patch(int x,int z,int cx,int cz,double radius,double noise)=>(x-cx)*(x-cx)+(z-cz)*(z-cz)<(radius+noise*.7)*(radius+noise*.7);
        public TerrainKind At(int x,int z)
        {
            if(x < -Radius || x > Radius || z < -Radius || z > Radius)return TerrainKind.Water;
            return _tiles[(z+Radius)*Size+x+Radius];
        }
        public TerrainKind At(Point p)=>At((int)Math.Floor(p.X+.5f),(int)Math.Floor(p.Z+.5f));
        public bool Land(Point p,float radius=0)
        {
            // 점유 원의 바운딩 박스가 닿는 타일까지 검사해 해안에 반쯤 걸쳐 생성되지 않는다.
            int minX=(int)Math.Floor(p.X-radius+.5f),maxX=(int)Math.Floor(p.X+radius+.5f);
            int minZ=(int)Math.Floor(p.Z-radius+.5f),maxZ=(int)Math.Floor(p.Z+radius+.5f);
            for(int z=minZ;z<=maxZ;z++)for(int x=minX;x<=maxX;x++)if(At(x,z)==TerrainKind.Water)return false;
            return true;
        }
    }
}
