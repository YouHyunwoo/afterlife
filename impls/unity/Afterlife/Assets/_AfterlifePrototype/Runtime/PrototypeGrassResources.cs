using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Devlog.Terrain;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private readonly Dictionary<Kind,HashSet<int>> _grassResourceSites=new Dictionary<Kind,HashSet<int>>();
        private readonly Dictionary<Kind,List<List<Point>>> _grassResourceGroups=new Dictionary<Kind,List<List<Point>>>();
        private void PlanGrassResources()
        {
            _grassResourceSites.Clear();_grassResourceGroups.Clear();
            foreach(var kind in new[]{Kind.Tree,Kind.Rock}){_grassResourceSites[kind]=new HashSet<int>();_grassResourceGroups[kind]=new List<List<Point>>();}
            var centers=new List<Point>();var candidates=new List<Point>();
            for(int z=-Terrain.Radius+3;z<Terrain.Radius-2;z++)for(int x=-Terrain.Radius+3;x<Terrain.Radius-2;x++)
                if(x*x+z*z>49 && Terrain.At(x,z)==TerrainKind.Grass)candidates.Add(new Point(x,z));
            candidates=candidates.OrderBy(p=>ShowcaseTileVariants.Seed(unchecked((int)p.X+Terrain.Seed),(int)p.Z+613)).ToList();
            int treeGroups=0,rockGroups=0;
            foreach(var center in candidates)
            {
                if(centers.Any(p=>Point.Distance(p,center)<5.5f))continue;
                int seed=ShowcaseTileVariants.Seed(unchecked((int)center.X+Terrain.Seed),(int)center.Z-313);
                Kind kind=seed%2==0?Kind.Tree:Kind.Rock;
                if(kind==Kind.Tree?treeGroups>=Math.Max(1,Settings.TreeLimit/12):rockGroups>=Math.Max(1,Settings.RockLimit/12))continue;
                var random=new Random(seed);var nearby=new List<Point>();
                for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)nearby.Add(new Point(center.X+dx,center.Z+dz));
                nearby=nearby.OrderBy(p=>random.Next()).Take(5+(seed/7)%2).ToList();
                if(nearby.Any(p=>Terrain.At(p)!=TerrainKind.Grass||!Walkable(p,.38f)||!UnitFree(p,0)))continue;
                _grassResourceGroups[kind].Add(nearby);centers.Add(center);
                foreach(var p in nearby)_grassResourceSites[kind].Add(TileKey((int)p.X,(int)p.Z));
                if(kind==Kind.Tree)treeGroups++;else rockGroups++;
            }
            // 군집 사이의 개체는 서로도 떨어뜨려 놓는다.
            var singles=new List<Point>();
            foreach(var point in candidates)
            {
                int seed=ShowcaseTileVariants.Seed(unchecked((int)point.X+Terrain.Seed),(int)point.Z+997);
                if(seed%100>=3 || centers.Any(p=>Point.Distance(p,point)<3.6f)||singles.Any(p=>Point.Distance(p,point)<3))continue;
                if(!Walkable(point,.38f)||!UnitFree(point,0))continue;
                Kind kind=seed%2==0?Kind.Tree:Kind.Rock;
                _grassResourceGroups[kind].Add(new List<Point>{point});_grassResourceSites[kind].Add(TileKey((int)point.X,(int)point.Z));singles.Add(point);
            }
        }
        private void SeedGrassResources(Kind kind,ref int added,int budget)
        {
            foreach(var group in _grassResourceGroups[kind].OrderBy(group=>group.Count==1?0:1))
            {
                // 배치 한도나 장애물 때문에 5~6개 군집이 일부만 생성되지 않게 묶음으로 검사한다.
                if(added+group.Count>budget || group.Any(p=>!Walkable(p,.38f)||!UnitFree(p,0)))continue;
                foreach(var p in group){AddResource(kind,p.X,p.Z);added++;}
            }
        }
    }
}
