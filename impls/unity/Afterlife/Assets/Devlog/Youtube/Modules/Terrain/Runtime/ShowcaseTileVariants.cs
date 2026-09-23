using System;

namespace Afterlife.Devlog.Terrain
{
    public enum TileDecoration { Empty, Pebbles, MossyRock, Flowers, Ferns, DirtClods }

    // Unity와 무관한 좌표별 고정 시드 카탈로그. 회전과 높이 조회도 같은 규칙을 사용한다.
    public static class ShowcaseTileVariants
    {
        public const int Segments = 4;
        public static int Seed(int x,int z)
        {
            unchecked { uint h=(uint)x*73856093u^(uint)z*19349663u^37129u; h=(h^(h>>13))*1274126177u; return (int)(h&0x7fffffffu); }
        }
        public static int Shape(int x,int z) => Seed(x,z)%16;
        public static int Rotation(int x,int z) => (Seed(x,z)/7)%4;
        public static TileDecoration Decoration(ShowcaseBiome biome,int x,int z)
        {
            int choice=(Seed(x,z)/31)%10;
            if (biome==ShowcaseBiome.Water || choice<4) return TileDecoration.Empty;
            if (biome==ShowcaseBiome.Dirt) return TileDecoration.DirtClods;
            switch (biome)
            {
                case ShowcaseBiome.Rocky: return choice<7 ? TileDecoration.Pebbles : TileDecoration.MossyRock;
                case ShowcaseBiome.Snow: return choice<8 ? TileDecoration.Pebbles : TileDecoration.Empty;
                case ShowcaseBiome.Sand: return choice<7 ? TileDecoration.Pebbles : TileDecoration.Empty;
                case ShowcaseBiome.Forest: return choice<7 ? TileDecoration.Ferns : TileDecoration.MossyRock;
                default: return choice<7 ? TileDecoration.Flowers : choice<9 ? TileDecoration.Pebbles : TileDecoration.Ferns;
            }
        }
        public static double NodeHeight(ShowcaseBiome biome,int shape,int x,int z)
        {
            if (x==0 || z==0 || x==Segments || z==Segments || biome==ShowcaseBiome.Water) return 0;
            double amplitude=biome==ShowcaseBiome.Rocky ? .14 : biome==ShowcaseBiome.Snow ? .09
                : biome==ShowcaseBiome.Sand ? .045 : biome==ShowcaseBiome.Dirt ? .035 : .08;
            double u=x/(double)Segments,v=z/(double)Segments;
            double envelope=Math.Sin(u*Math.PI)*Math.Sin(v*Math.PI);
            double variation=.18+(Seed(x+shape*11,z+shape*3)%1000)/850.0;
            return amplitude*envelope*variation;
        }
        public static double NodeU(int shape,int x,int z) => x/(double)Segments+
            (x==0 || z==0 || x==Segments || z==Segments ? 0 : ((Seed(x+shape*17,z+91)%1000)/999.0-.5)*.13);
        public static double NodeV(int shape,int x,int z) => z/(double)Segments+
            (x==0 || z==0 || x==Segments || z==Segments ? 0 : ((Seed(x+73,z+shape*19)%1000)/999.0-.5)*.13);
        public static bool AlternateDiagonal(int shape,int x,int z) => Seed(x+shape*5,z+39)%2==0;
        public static double Height(int x,int z,double u,double v) => ShowcaseTerrainField.Height(x+u,z+v);
        // 보관된 이전 메시를 새 높이장으로 이관할 때만 사용한다.
        public static double LegacyHeight(int x,int z,double u,double v)
        {
            // 월드 좌표에서 90도 단위 타일 회전을 역변환한다.
            double px=u-.5,pz=v-.5;
            for (int i=0;i<Rotation(x,z);i++) { double old=px; px=-pz; pz=old; }
            return LocalHeight(ShowcaseBiomeMap.At(x,z),Shape(x,z),px+.5,pz+.5);
        }
        public static double LocalHeight(ShowcaseBiome biome,int shape,double u,double v)
        {
            double Sample(int ax,int az,int bx,int bz,int cx,int cz)
            {
                double a=NodeU(shape,ax,az),b=NodeV(shape,ax,az),c=NodeU(shape,bx,bz),d=NodeV(shape,bx,bz);
                double e=NodeU(shape,cx,cz),f=NodeV(shape,cx,cz);
                double denominator=(d-f)*(a-e)+(e-c)*(b-f);
                double wa=((d-f)*(u-e)+(e-c)*(v-f))/denominator;
                double wb=((f-b)*(u-e)+(a-e)*(v-f))/denominator, wc=1-wa-wb;
                return wa>=-.000001 && wb>=-.000001 && wc>=-.000001
                    ? wa*NodeHeight(biome,shape,ax,az)+wb*NodeHeight(biome,shape,bx,bz)+wc*NodeHeight(biome,shape,cx,cz) : double.NaN;
            }
            for (int z=0;z<Segments;z++)
            for (int x=0;x<Segments;x++)
            {
                bool alternate=AlternateDiagonal(shape,x,z);
                double height=alternate ? Sample(x,z,x,z+1,x+1,z) : Sample(x,z,x,z+1,x+1,z+1);
                if (!double.IsNaN(height)) return height;
                height=alternate ? Sample(x+1,z,x,z+1,x+1,z+1) : Sample(x,z,x+1,z+1,x+1,z);
                if (!double.IsNaN(height)) return height;
            }
            return 0;
        }
    }
}
