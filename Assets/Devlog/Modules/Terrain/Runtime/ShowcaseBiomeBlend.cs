using System;

namespace Afterlife.Devlog.Terrain
{
    // 지형 전체가 같은 좌표장을 사용하므로 타일 회전/경계에서 무늬가 끊기지 않는다.
    public static class ShowcaseBiomeBlend
    {
        public static void Weights(double x, double z, double[] weights)
        {
            Array.Clear(weights,0,weights.Length);
            double px=x+(Noise(x*2.3+17,z*2.3+31)-.5)*.34+(Noise(x*8+9,z*8)-.5)*.09;
            double pz=z+(Noise(x*2.3+73,z*2.3+11)-.5)*.34+(Noise(x*8,z*8+43)-.5)*.09;
            int ix=(int)Math.Floor(px-.5), iz=(int)Math.Floor(pz-.5);
            double u=Smooth((px-.5-ix-.20)/.60),v=Smooth((pz-.5-iz-.20)/.60),total=0;
            for (int b=0;b<2;b++)
            for (int a=0;a<2;a++)
            {
                var biome=ShowcaseBiomeMap.At(Math.Max(-6,Math.Min(5,ix+a)),Math.Max(-5,Math.Min(4,iz+b)));
                if (biome==ShowcaseBiome.Water) continue;
                double weight=(a==0 ? 1-u : u)*(b==0 ? 1-v : v);
                weights[(int)biome]+=weight; total+=weight;
            }
            if (total>1e-9) for (int i=0;i<weights.Length;i++) weights[i]/=total;
            else weights[(int)ShowcaseBiome.Sand]=1;
        }

        private static double Smooth(double t) { t=Math.Max(0,Math.Min(1,t)); return t*t*(3-2*t); }
        private static double Noise(double x,double z)
        {
            int ix=(int)Math.Floor(x),iz=(int)Math.Floor(z);
            double u=Smooth(x-ix),v=Smooth(z-iz);
            double a=Hash(ix,iz)*(1-u)+Hash(ix+1,iz)*u;
            double b=Hash(ix,iz+1)*(1-u)+Hash(ix+1,iz+1)*u;
            return a*(1-v)+b*v;
        }
        private static double Hash(int x,int z) => (ShowcaseTileVariants.Seed(x,z)%65536)/65535.0;
    }
}
