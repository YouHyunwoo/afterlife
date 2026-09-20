using System;

namespace Afterlife.Devlog.Terrain
{
    public enum CoastShape { Square, Bevel, Rounded }
    public sealed class CoastalTerrain
    {
        public CoastShape Shape { get; }
        public float WaterHeight { get; }
        public float FloorHeight { get; }
        public CoastalTerrain(CoastShape shape,float waterHeight,float floorHeight)
        { Shape=shape;WaterHeight=waterHeight;FloorHeight=floorHeight; }
    }
    public static class CoastField
    {
        public const double Width=.22;
        // 정수 모서리 좌표계. 지도 종류와 크기는 호출자가 주입한다.
        public static double Offset(double px,double pz,Func<int,int,bool> water,CoastShape shape)
        {
            if(shape==CoastShape.Square)return 0;
            double distance=Width;int cx=(int)Math.Floor(px),cz=(int)Math.Floor(pz);
            for(int z=cz-1;z<=cz+1;z++)for(int x=cx-1;x<=cx+1;x++)
            {
                if(!water(x,z))continue;
                double dx=Math.Max(0,Math.Max(x-px,px-x-1)),dz=Math.Max(0,Math.Max(z-pz,pz-z-1));
                distance=Math.Min(distance,Math.Sqrt(dx*dx+dz*dz));
            }
            if(shape==CoastShape.Bevel)return distance-Width;
            double horizontal=Width-distance;
            return Math.Sqrt(Math.Max(0,Width*Width-horizontal*horizontal))-Width;
        }
    }
}
