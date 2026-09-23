using System;

namespace Afterlife.Devlog.Terrain
{
    public enum ShowcaseBiome { Grass, Dirt, Sand, Water, Snow, Forest, Rocky }

    // 촬영용 고정 소형 지도. 세 단계가 동일한 바이옴/격자 데이터를 공유한다.
    public static class ShowcaseBiomeMap
    {
        public const int Width = 12, Depth = 10;
        public static ShowcaseBiome At(int x, int z)
        {
            double px = x + .5, pz = z + .5;
            double noise = (Math.Sin(px*.73 + Math.Sin(pz*.6)) + Math.Sin(pz*.91+px*.17))*.5;
            double coast = px - 3.2 + (-pz-3.2)*.85 + noise*.3;
            if (pz < -2 && coast > .7) return ShowcaseBiome.Water;
            if (pz < -1.8 && coast > -.5) return ShowcaseBiome.Sand;
            if (pz > 3.3 && px < -.5) return ShowcaseBiome.Snow;
            if (px < -2 && pz > -.3 + noise*.4) return ShowcaseBiome.Forest;
            // 맵 남쪽에서 집 현관(2.35, 1.3) 앞까지 연결되는 격자 길.
            bool path = (x == -1 && z <= -1) || (x == 0 && z == -1)
                || (z == 0 && x >= 0 && x <= 2) || (x == 2 && z == 1);
            if (path) return ShowcaseBiome.Dirt;
            if (px > 1.5 && pz < .3) return ShowcaseBiome.Rocky;
            return ShowcaseBiome.Grass;
        }
    }
}
