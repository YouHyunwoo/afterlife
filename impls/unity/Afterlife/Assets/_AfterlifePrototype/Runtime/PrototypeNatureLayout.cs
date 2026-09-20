using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Devlog.Terrain;

namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private bool PreferredResourceBiome(Kind kind,Point p)=>Terrain.At(p)==(kind==Kind.Tree?TerrainKind.Forest:TerrainKind.Rocky);
        private bool InResourceCluster(Kind kind,Point p)
        {
            int x=(int)Math.Round(p.X),z=(int)Math.Round(p.Z);
            var biome=Terrain.At(p);
            if(biome==TerrainKind.Dirt || biome==TerrainKind.Water)return false;
            if(PreferredResourceBiome(kind,p))
                return ShowcaseTileVariants.Seed(unchecked(x+Terrain.Seed),z-71)%100<90;
            if(biome==TerrainKind.Grass)return _grassResourceSites.TryGetValue(kind,out var sites)&&sites.Contains(TileKey(x,z));
            // 숲/암석 타일의 자리는 각각 나무/바위를 위해 남긴다.
            if(biome==TerrainKind.Forest || biome==TerrainKind.Rocky)return false;
            return ShowcaseResourceClusters.Allows(kind==Kind.Tree,x,z,(ShowcaseBiome)(int)biome,3);
        }
        private void SeedBiomes()
        {
            PlanGrassResources();
            SeedResourceClusters(Kind.Tree,Settings.TreeLimit);
            SeedResourceClusters(Kind.Rock,Settings.RockLimit);
        }
        private void SeedResourceClusters(Kind kind,int limit)
        {
            var candidates=new List<Point>();
            for(int z=-Settings.MapRadius+2;z<Settings.MapRadius-1;z++)
            for(int x=-Settings.MapRadius+2;x<Settings.MapRadius-1;x++)
            {
                var p=new Point(x,z);
                if(Terrain.At(p)==TerrainKind.Grass || Point.Distance(p,new Point(0,0))<5 || !InResourceCluster(kind,p))continue;
                candidates.Add(p);
            }
            // 행 스캔 순서로 남쪽부터 채우지 않고 지도 전체를 고정 시드로 섞는다.
            candidates=candidates.OrderBy(p=>ShowcaseTileVariants.Seed((int)p.X+43,(int)p.Z-97)).ToList();
            int budget=Math.Max(candidates.Count(p=>PreferredResourceBiome(kind,p)),Math.Max(0,limit-Bodies.Count(b=>b.Alive&&b.Kind==kind)));
            int snowBudget=Math.Max(1,budget/5),added=0,snowAdded=0;
            var used=new HashSet<int>();
            for(int pass=0;pass<3;pass++)
            {
                if(pass==2)SeedGrassResources(kind,ref added,budget);
                foreach(var p in candidates)
                {
                if(added>=budget)break;
                if(pass==0 && !PreferredResourceBiome(kind,p))continue;
                if(pass==1 && (Terrain.At(p)!=TerrainKind.Snow || snowAdded>=snowBudget))continue;
                int key=TileKey((int)p.X,(int)p.Z);
                if(used.Contains(key) || !Walkable(p,.38f) || !UnitFree(p,0))continue;
                AddResource(kind,p.X,p.Z);used.Add(key);added++;if(Terrain.At(p)==TerrainKind.Snow)snowAdded++;
                }
            }
        }
    }
}
