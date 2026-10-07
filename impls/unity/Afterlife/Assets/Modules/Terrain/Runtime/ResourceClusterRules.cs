using System;
namespace Afterlife.Modules.Terrain
{
    // 고정 시드 군집 분포. 게임 개체/난수/Unity API를 참조하지 않는다.
    public static class ResourceClusterRules
    {
        public static bool Allows(bool wood,int x,int z,TerrainBiome biome,double densityMultiplier=1)
        {
            if(biome==TerrainBiome.Water)return false;
            int salt=wood?193:617;
            int cx=(int)Math.Floor(x/9.0),cz=(int)Math.Floor(z/9.0);
            double density=0;
            for(int j=-1;j<=1;j++)for(int i=-1;i<=1;i++)
            {
                int seed=TerrainTileVariants.Seed(cx+i+salt,cz+j-salt);
                double px=(cx+i)*9+2+(seed%500)/100.0,pz=(cz+j)*9+2+((seed/503)%500)/100.0;
                double radius=wood?3.8:3.1;
                double d2=(x-px)*(x-px)+(z-pz)*(z-pz);
                density=Math.Max(density,Math.Max(0,1-d2/(radius*radius)));
            }
            double weight=biome==TerrainBiome.Sand?(wood?.15:.5):biome==TerrainBiome.Forest?(wood?1.25:.6):biome==TerrainBiome.Rocky?(wood?.4:1.25):1;
            return (TerrainTileVariants.Seed(x+salt*2,z-salt)%10000)/10000.0<density*.88*weight*Math.Max(0,densityMultiplier);
        }
    }
}
