using System;

namespace Afterlife.Devlog.Terrain
{
    // 맵 전체 좌표의 gradient Perlin. 타일 경계나 회전은 높이장에 관여하지 않는다.
    public static class ShowcaseTerrainField
    {
        public const int Segments=4;
        private static double Fade(double t) => t*t*t*(t*(t*6-15)+10);
        private static double Lerp(double a,double b,double t) => a+(b-a)*t;
        private static double Gradient(int x,int z,double dx,double dz)
        {
            switch(ShowcaseTileVariants.Seed(x,z)&7)
            {
                case 0:return dx; case 1:return -dx; case 2:return dz; case 3:return -dz;
                case 4:return (dx+dz)*.70710678118; case 5:return (dx-dz)*.70710678118;
                case 6:return (-dx+dz)*.70710678118; default:return (-dx-dz)*.70710678118;
            }
        }
        private static double Perlin(double x,double z)
        {
            int ix=(int)Math.Floor(x),iz=(int)Math.Floor(z); double u=x-ix,v=z-iz;
            return Lerp(Lerp(Gradient(ix,iz,u,v),Gradient(ix+1,iz,u-1,v),Fade(u)),
                Lerp(Gradient(ix,iz+1,u,v-1),Gradient(ix+1,iz+1,u-1,v-1),Fade(u)),Fade(v));
        }
        public static double Node(double x,double z)
        {
            // 큰 굴곡과 작은 면 변화를 섞되 특정 타일의 경계에서 감쇠하지 않는다.
            double broad=Perlin(x*.43+17.31,z*.43+29.17);
            double detail=Perlin(x*1.27+53.47,z*1.27+11.83);
            double fine=Perlin(x*2.61+7.19,z*2.61+83.61);
            return .08+broad*.16+detail*.075+fine*.025;
        }
        public static bool Alternate(int cellX,int cellZ) => (ShowcaseTileVariants.Seed(cellX,cellZ)&1)==0;
        // 실제 생성한 1/4m 삼각형을 보간해 풀과 자원의 밑면을 메시 위에 맞춘다.
        public static double Height(double x,double z)
        {
            int ix=(int)Math.Floor(x*Segments),iz=(int)Math.Floor(z*Segments);
            double u=x*Segments-ix,v=z*Segments-iz;
            double a=Node(ix/(double)Segments,iz/(double)Segments);
            double b=Node(ix/(double)Segments,(iz+1)/(double)Segments);
            double c=Node((ix+1)/(double)Segments,iz/(double)Segments);
            double d=Node((ix+1)/(double)Segments,(iz+1)/(double)Segments);
            if(Alternate(ix,iz)) return u+v<=1 ? a+(c-a)*u+(b-a)*v : d+(b-d)*(1-u)+(c-d)*(1-v);
            return v>=u ? a+(d-b)*u+(b-a)*v : a+(c-a)*u+(d-c)*v;
        }
    }
}
