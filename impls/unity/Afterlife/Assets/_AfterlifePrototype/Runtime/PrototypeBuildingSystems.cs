using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Buildings;

namespace Afterlife.Prototype
{
    // Game adapter: the independent Buildings module owns connectivity, merge search and cycle math.
    public sealed partial class VillageModel
    {
        private readonly TownNetwork _towns=new TownNetwork();
        private readonly Dictionary<int,ProductionCycle> _production=new Dictionary<int,ProductionCycle>();
        public bool HasAltar => Has(Kind.Altar)||Has(Kind.Sanctuary);
        public static bool IsHousing(Kind kind) => kind==Kind.House||kind==Kind.TownHall;
        public bool IsProductionBuilding(Kind kind) => Building(kind).Production!=ProductionKind.None;
        public bool TrySpendEtheron(int amount)
        { if(Frozen||amount<0||Etheron<amount)return false;Etheron-=amount;return true; }
        private static BuildingFootprint Footprint(Body body) => new BuildingFootprint((int)Math.Round(body.Position.X-(body.Width-1)*.5f),(int)Math.Round(body.Position.Z-(body.Depth-1)*.5f),body.Width,body.Depth);
        public float LocalConstructionMultiplier(Body body) => _towns.ConstructionMultiplier(Footprint(body));
        private void ResetBuildingSystems() { _production.Clear();_towns.Rebuild(Array.Empty<TownSource>()); }
        private ProductionCycle Cycle(int id)
        { if(!_production.TryGetValue(id,out var cycle)){cycle=new ProductionCycle();_production.Add(id,cycle);}return cycle; }
        public int OwnedTroopCount(int id) => Bodies.Count(b=>b.Alive&&b.Friendly&&b.Owner==id);
        public float ProductionProgress(int id) => Find(id)!=null&&_production.TryGetValue(id,out var cycle)?cycle.Progress:0;
        public float ProductionRemaining(int id)
        { var body=Find(id);return body==null||!IsProductionBuilding(body.Kind)?0:(1-ProductionProgress(id))*ProductionDuration(body); }
        private float ProductionDuration(Body body)
        {
            float speed=ProductionSpeed;
            if(body.Kind==Kind.Farm)
            {
                var area=Footprint(body);
                int count=Bodies.Count(other=>other!=body&&other.Alive&&other.Complete&&!other.MovingOut&&other.MoveSource==0&&other.Kind==Kind.Farm&&area.Touches(Footprint(other)));
                speed*=BuildingMerge.AdjacencySpeed(count);
            }
            return Math.Max(.01f,Building(body.Kind).ProductionSeconds/Math.Max(.01f,speed));
        }
        private bool CanProduce(Body body)
        {
            if(body==null||!body.Alive||!body.Complete||body.MovingOut||body.MoveSource!=0)return false;
            var stats=Building(body.Kind);
            return stats.Production!=ProductionKind.None&&(stats.Production!=ProductionKind.Troop||OwnedTroopCount(body.Id)<stats.TroopCapacity);
        }
        public bool AccelerateProduction(int id)
        {
            var body=Find(id);if(Frozen||!CanProduce(body))return false;
            var stats=Building(body.Kind);if(stats.AccelerationGoldCost<=0||Money<stats.AccelerationGoldCost||ProductionRemaining(id)<=0)return false;
            Money-=stats.AccelerationGoldCost;
            float duration=ProductionDuration(body);
            if(Cycle(id).Advance(Math.Min(1,Cycle(id).Remaining(duration)),duration))TryFinishProduction(body);
            return true;
        }
        private void TickBuildingProduction(float dt)
        {
            foreach(var body in Bodies.ToArray())
            {
                if(!body.Building||!body.Alive)continue;
                if(!CanProduce(body))
                {
                    if(body.Complete&&!body.MovingOut&&Building(body.Kind).Production==ProductionKind.Troop&&OwnedTroopCount(body.Id)>=Building(body.Kind).TroopCapacity)Cycle(body.Id).SetProgress(0);
                    continue;
                }
                var cycle=Cycle(body.Id);
                // A blocked spawn keeps one completed cycle but never accrues an unbounded backlog.
                if(!cycle.Ready)cycle.Advance(dt,ProductionDuration(body));
                while(cycle.Ready)
                {
                    if(!TryFinishProduction(body)){cycle.SetProgress(1);break;}
                    if(!CanProduce(body)){cycle.SetProgress(0);break;}
                }
            }
        }
        private bool TryFinishProduction(Body body)
        {
            var stats=Building(body.Kind);
            if(stats.Production==ProductionKind.Troop)
            {
                Point? spawn=null;
                for(int i=0;i<80;i++)
                {
                    float angle=i*(float)Math.PI/8, radius=Math.Max(body.Width,body.Depth)*.5f+.6f+i/16*.65f;
                    var point=new Point(body.Position.X+(float)Math.Cos(angle)*radius,body.Position.Z+(float)Math.Sin(angle)*radius);
                    if(Walkable(point,.34f)&&UnitFree(point,0)){spawn=point;break;}
                }
                if(!spawn.HasValue)return false;
                var unit=Add(stats.ProducedUnit,spawn.Value,TroopHealth(stats.ProducedUnit));unit.Owner=body.Id;unit.Rally=body.Rally;_tacticsClock=0;
                Sound(SoundCue.Complete);
            }
            else
            {
                int amount=stats.ProductionAmount*ProductionYieldMultiplier(body);
                if(stats.Production==ProductionKind.Gold){Money+=amount;EmitText(body,$"보리 +{amount} · {amount}원");}
                else if(stats.Production==ProductionKind.Etheron){Etheron+=amount;EmitText(body,$"에테론 +{amount}");}
            }
            Cycle(body.Id).Complete();return true;
        }
        private void RebuildTowns()
        {
            var sources=new List<TownSource>();
            foreach(var body in Bodies)if(body.Alive&&body.Complete&&!body.MovingOut&&body.MoveSource==0&&IsHousing(body.Kind))
            {var stats=Building(body.Kind);sources.Add(new TownSource(Footprint(body),stats.TerritoryRadius,stats.ConstructionSpeedBonus));}
            _towns.Rebuild(sources);var tiles=new HashSet<int>();
            foreach(var tile in _towns.Tiles)if(Math.Abs(tile.X)<Settings.MapRadius&&Math.Abs(tile.Z)<Settings.MapRadius)tiles.Add(TileKey(tile.X,tile.Z));
            if(!Territory.SetEquals(tiles)){Territory.Clear();Territory.UnionWith(tiles);TerritoryRevision++;}
            Population=Capacity=0;UpdateCapturedSites();
        }
        private bool MergeBuildings()
        {
            bool changed=false;
            while(true)
            {
                var candidates=new List<MergeCandidate>();
                foreach(var body in Bodies)
                {
                    if(!body.Alive||!body.Complete||body.MovingOut||body.MoveSource!=0||body.Width!=1||body.Depth!=1)continue;
                    var stats=Building(body.Kind);if(!stats.CanMerge)continue;
                    var area=Footprint(body);candidates.Add(new MergeCandidate(body.Id,(int)body.Kind,(int)stats.MergeTarget,area.X,area.Z));
                }
                var ids=BuildingMerge.FindSquare(candidates);if(ids==null)break;
                var parts=ids.Select(Find).ToArray();var first=parts[0];var origin=Footprint(first);
                float health=parts.Sum(b=>b.Health),maximum=parts.Sum(b=>b.MaxHealth),progress=parts.Average(b=>ProductionProgress(b.Id));
                var merged=AddBuilding(Building(first.Kind).MergeTarget,origin.X,origin.Z,true);
                merged.Width=merged.Depth=2;merged.Position=new Point(origin.X+.5f,origin.Z+.5f);merged.Health=health;merged.MaxHealth=maximum;
                Cycle(merged.Id).SetProgress(progress);
                foreach(var part in parts){part.Health=0;Bodies.Remove(part);_production.Remove(part.Id);Selected.Remove(part.Id);}
                changed=true;TopologyRevision++;_obstaclesDirty=true;Sound(SoundCue.Complete);
            }
            if(changed)InvalidatePaths();return changed;
        }
        private void TransferProduction(int source,int target)
        { if(_production.TryGetValue(source,out var cycle)){_production[target]=cycle;_production.Remove(source);} }
    }
}
