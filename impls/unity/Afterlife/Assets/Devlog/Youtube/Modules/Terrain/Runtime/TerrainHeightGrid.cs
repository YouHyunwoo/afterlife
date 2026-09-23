using System;

namespace Afterlife.Devlog.Terrain
{
    // 순수 C# 높이 캐시. 메시와 배치 조회가 동일한 정점 및 대각선을 사용한다.
    public sealed class TerrainHeightGrid
    {
        private readonly double[] _heights;
        private readonly int _start,_size;
        public TerrainHeightGrid(int radius)
        {
            _start=(-radius*2-1)*2;
            _size=(radius*2+1)*ShowcaseTerrainField.Segments+1;
            _heights=new double[_size*_size];
            for(int z=0;z<_size;z++) for(int x=0;x<_size;x++)
                _heights[z*_size+x]=ShowcaseTerrainField.Node((_start+x)/4.0,(_start+z)/4.0);
        }
        public double Node(int x,int z)
        {
            int ix=x-_start,iz=z-_start;
            return ix>=0 && iz>=0 && ix<_size && iz<_size ? _heights[iz*_size+ix] : ShowcaseTerrainField.Node(x/4.0,z/4.0);
        }
        public double Height(double x,double z)
        {
            int ix=(int)Math.Floor(x*4),iz=(int)Math.Floor(z*4);
            double u=x*4-ix,v=z*4-iz;
            double a=Node(ix,iz),b=Node(ix,iz+1),c=Node(ix+1,iz),d=Node(ix+1,iz+1);
            if(ShowcaseTerrainField.Alternate(ix,iz)) return u+v<=1 ? a+(c-a)*u+(b-a)*v : d+(b-d)*(1-u)+(c-d)*(1-v);
            return v>=u ? a+(d-b)*u+(b-a)*v : a+(c-a)*u+(d-c)*v;
        }
    }
}
