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
        public int Seed { get; private set; }
        public int Revision { get; private set; }
        public Afterlife.Modules.Terrain.WorldMapLayout Layout { get; private set; }
        public TerrainMap(int radius){Radius=Math.Max(8,radius);_tiles=new TerrainKind[Size*Size];}
        public void Regenerate(int seed)
        {
            Seed=seed==0?Guid.NewGuid().GetHashCode():seed;
            Layout=new Afterlife.Modules.Terrain.WorldMapLayout(Radius,Seed);
            for(int i=0;i<_tiles.Length;i++)_tiles[i]=(TerrainKind)(int)Layout.Tiles[i];
            Revision++;
        }
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
