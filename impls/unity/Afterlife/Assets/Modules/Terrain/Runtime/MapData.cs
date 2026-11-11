using System;
using System.Collections.Generic;

namespace Afterlife.Modules.Terrain
{
    // 맵 결과는 생성기, 에셋, 게임 오브젝트의 수명과 독립적이다.
    [Serializable]
    public sealed class MapData
    {
        public int Radius;
        public TerrainBiome[] Tiles;
        public double[] Heights;
        public bool UseHeightSamples;
        public int Size => Radius * 2 + 1;
        public MapData(int radius)
        {
            if(radius<1 || radius>256) throw new ArgumentOutOfRangeException(nameof(radius));
            Radius=radius; Tiles=new TerrainBiome[Size*Size]; Heights=new double[Tiles.Length];
        }
        public TerrainBiome At(int x,int z) => Math.Abs(x)>Radius || Math.Abs(z)>Radius ? TerrainBiome.Water : Tiles[(z+Radius)*Size+x+Radius];
        public double Height(double x,double z)
        {
            if(!UseHeightSamples)return TerrainHeightField.Node(x,z);
            int ix=Math.Max(-Radius,Math.Min(Radius,(int)Math.Floor(x))),iz=Math.Max(-Radius,Math.Min(Radius,(int)Math.Floor(z)));
            int nx=Math.Min(Radius,ix+1),nz=Math.Min(Radius,iz+1);double u=Math.Max(0,Math.Min(1,x-ix)),v=Math.Max(0,Math.Min(1,z-iz));
            double a=Heights[(iz+Radius)*Size+ix+Radius],b=Heights[(iz+Radius)*Size+nx+Radius],c=Heights[(nz+Radius)*Size+ix+Radius],d=Heights[(nz+Radius)*Size+nx+Radius];
            return (a*(1-u)+b*u)*(1-v)+(c*(1-u)+d*u)*v;
        }
        public MapData Copy() => new MapData(Radius) { Tiles=(TerrainBiome[])Tiles.Clone(), Heights=(double[])Heights.Clone(),UseHeightSamples=UseHeightSamples };
    }
    public enum MapShape { Continent, SmallIsland }
    [Serializable]
    public sealed class MapGenerationSettings
    {
        public int Radius=7, Seed=1;
        public MapShape Shape=MapShape.SmallIsland;
        public bool Beach=true;
        public double GrassRadiusSquared=4.3, BeachRadiusSquared=10.7;
    }
    public static class MapGenerator
    {
        public static MapData Generate(MapGenerationSettings settings)
        {
            if(settings==null) throw new ArgumentNullException(nameof(settings));
            var result=new MapData(settings.Radius);
            if(settings.Shape==MapShape.Continent)
            {
                var layout=new WorldMapLayout(settings.Radius,settings.Seed,settings.Beach);
                Array.Copy(layout.Tiles,result.Tiles,result.Tiles.Length);
            }
            else for(int z=-result.Radius;z<=result.Radius;z++) for(int x=-result.Radius;x<=result.Radius;x++)
            {
                double r=x*x/1.18+z*z/.92;
                result.Tiles[(z+result.Radius)*result.Size+x+result.Radius]=r<settings.GrassRadiusSquared ? TerrainBiome.Grass :
                    r<settings.BeachRadiusSquared ? (settings.Beach?TerrainBiome.Sand:TerrainBiome.Grass) : TerrainBiome.Water;
            }
            return result;
        }
    }
    public struct PlacementData { public double X,Y,Z,Yaw,Scale; }
    public static class SurfaceScatter
    {
        public static PlacementData[] Generate(int radius,int count,int seed,Func<int,int,TerrainBiome> biome,Func<double,double,double> height,TerrainBiome allowed)
        {
            var result=new List<PlacementData>(); var random=new Random(seed);
            for(int i=0;i<Math.Max(0,count)*8 && result.Count<count;i++)
            {
                double x=(random.NextDouble()*2-1)*radius,z=(random.NextDouble()*2-1)*radius;
                if(biome((int)Math.Floor(x+.5),(int)Math.Floor(z+.5))!=allowed)continue;
                result.Add(new PlacementData { X=x,Y=height(x,z),Z=z,Yaw=random.NextDouble()*360,Scale=.8+random.NextDouble()*.4 });
            }
            return result.ToArray();
        }
    }
}
