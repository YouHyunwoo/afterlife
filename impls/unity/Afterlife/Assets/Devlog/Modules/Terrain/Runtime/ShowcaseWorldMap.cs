using System;
using System.Collections.Generic;
namespace Afterlife.Devlog.Terrain
{
    // 시드 기반 지형/수계/육지 경로 생성. 렌더링 및 게임 개체와 독립적이다.
    public sealed class ShowcaseWorldMap
    {
        public readonly int Radius,Size,Seed;
        public readonly ShowcaseBiome[] Tiles;
        public readonly bool[] Roads;
        public readonly List<(int X,int Z)> Landmarks=new List<(int,int)>();
        private readonly Random _random;
        public ShowcaseWorldMap(int radius,int seed)
        {
            Radius=radius;Size=radius*2+1;Seed=seed;Tiles=new ShowcaseBiome[Size*Size];Roads=new bool[Tiles.Length];_random=new Random(seed);
            double phase=_random.NextDouble()*30;
            for(int z=-radius;z<=radius;z++)for(int x=-radius;x<=radius;x++)
            {
                double coast=radius*.72+Math.Sin(z*.11+phase)*2.4+Noise(x*.1,z*.1)*2;
                var biome=x>coast?ShowcaseBiome.Water:x>coast-2?ShowcaseBiome.Sand:
                    z>radius*.54+Noise(x*.09,z*.09)*5?ShowcaseBiome.Snow:ShowcaseBiome.Grass;
                Tiles[Index(x,z)]=biome;
            }
            for(int i=0;i<9;i++)
            {
                int x=Range(-radius+5,(int)(radius*.55)),z=Range(-radius+5,radius-5);
                double spread=radius*(i<5?.12:.085);
                PaintPatch(x,z,Math.Max(2,spread),i<5?ShowcaseBiome.Forest:ShowcaseBiome.Rocky);
            }
            int lakeX=-(int)(radius*.42),lakeZ=(int)(radius*(.34+_random.NextDouble()*.18));
            Lake(lakeX,lakeZ,Math.Max(2,radius*.09),phase);
            Lake(-(int)(radius*.52),-(int)(radius*(.35+_random.NextDouble()*.18)),Math.Max(2,radius*.07),phase+7);
            // 호수에서 동쪽 바다까지. 내륙 시작점이 있어 강을 돌아서 건널 수 있다.
            
            for(int i=1;i<=Size*3;i++)
            {
                double t=i/(double)(Size*3),x=lakeX+(radius-lakeX)*t;
                double z=lakeZ+(radius*.12-lakeZ)*t+Math.Sin(t*Math.PI*3+phase)*2*Math.Sin(t*Math.PI);
                PaintWater(x,z,1.05+.35*Math.Sin(t*17+phase));
            }
            // 시작 지점은 자원 접근/건설이 가능한 마른 육지로 보장한다.
            for(int z=-Math.Min(5,radius);z<=Math.Min(5,radius);z++)for(int x=-Math.Min(5,radius);x<=Math.Min(5,radius);x++)Tiles[Index(x,z)]=ShowcaseBiome.Grass;
            AddShore();
            var reachable=Reachable();
            for(int i=0;i<4;i++)
            {
                int chosen=-1;double best=-1;
                for(int attempt=0;attempt<200;attempt++)
                {
                    int x=Range(-radius+3,(int)(radius*.6)),z=Range(-radius+3,radius-3),index=Index(x,z);
                    if(!reachable[index] || x*x+z*z<64 || !DryArea(x,z))continue;
                    // 네 방향의 목적지를 선택해 한쪽으로만 뻗는 길을 피한다.
                    if((i%2==0?x>=0:x<=0) || (i<2?z>=0:z<=0))continue;
                    double score=x*x+z*z;
                    foreach(var other in Landmarks)score=Math.Min(score,(x-other.X)*(x-other.X)+(z-other.Z)*(z-other.Z));
                    if(score>best){best=score;chosen=index;}
                }
                if(chosen<0)continue;
                int px=chosen%Size-radius,pz=chosen/Size-radius;
                Landmarks.Add((px,pz));CarveRoad(px,pz);
            }
        }
        private int Range(int min,int max)=>max<=min?min:_random.Next(min,max+1);
        private int Index(int x,int z)=>(z+Radius)*Size+x+Radius;
        private bool Inside(int x,int z)=>x>=-Radius&&x<=Radius&&z>=-Radius&&z<=Radius;
        private double Hash(int x,int z)=>ShowcaseTileVariants.Seed(unchecked(x+Seed),unchecked(z-Seed))/(double)int.MaxValue;
        private double Noise(double x,double z)
        {
            int ix=(int)Math.Floor(x),iz=(int)Math.Floor(z);double u=x-ix,v=z-iz;u=u*u*(3-2*u);v=v*v*(3-2*v);
            return (Hash(ix,iz)*(1-u)+Hash(ix+1,iz)*u)*(1-v)+(Hash(ix,iz+1)*(1-u)+Hash(ix+1,iz+1)*u)*v;
        }
        private void PaintPatch(int cx,int cz,double radius,ShowcaseBiome biome)
        {
            for(int z=Math.Max(-Radius,cz-(int)radius-2);z<=Math.Min(Radius,cz+(int)radius+2);z++)
            for(int x=Math.Max(-Radius,cx-(int)radius-2);x<=Math.Min(Radius,cx+(int)radius+2);x++)
            {
                var old=Tiles[Index(x,z)];if(old==ShowcaseBiome.Water||old==ShowcaseBiome.Sand||old==ShowcaseBiome.Snow)continue;
                double r=radius+(Noise(x*.4,z*.4)-.5)*2;
                if((x-cx)*(x-cx)+(z-cz)*(z-cz)<r*r)Tiles[Index(x,z)]=biome;
            }
        }
        private void Lake(int cx,int cz,double radius,double phase)
        {
            for(int z=-Radius;z<=Radius;z++)for(int x=-Radius;x<=Radius;x++)
            {
                double angle=Math.Atan2(z-cz,x-cx),r=radius*(1+.13*Math.Sin(angle*3+phase)+.09*Math.Cos(angle*5));
                if((x-cx)*(x-cx)+(z-cz)*(z-cz)*1.35<r*r)Tiles[Index(x,z)]=ShowcaseBiome.Water;
            }
        }
        private void PaintWater(double bx,double bz,double width)
        {
            for(int z=(int)Math.Floor(bz-width);z<=Math.Ceiling(bz+width);z++)for(int x=(int)Math.Floor(bx-width);x<=Math.Ceiling(bx+width);x++)
                if(Inside(x,z)&&(x-bx)*(x-bx)+(z-bz)*(z-bz)<=width*width)Tiles[Index(x,z)]=ShowcaseBiome.Water;
        }
        private void AddShore()
        {
            var old=(ShowcaseBiome[])Tiles.Clone();
            for(int z=-Radius+1;z<Radius;z++)for(int x=-Radius+1;x<Radius;x++)
            {
                int index=Index(x,z);if(old[index]==ShowcaseBiome.Water||old[index]==ShowcaseBiome.Snow)continue;
                if(old[index-1]==ShowcaseBiome.Water||old[index+1]==ShowcaseBiome.Water||old[index-Size]==ShowcaseBiome.Water||old[index+Size]==ShowcaseBiome.Water)Tiles[index]=ShowcaseBiome.Sand;
            }
        }
        private bool DryArea(int x,int z)
        {
            for(int dz=-1;dz<=1;dz++)for(int dx=-1;dx<=1;dx++)if(!Inside(x+dx,z+dz)||Tiles[Index(x+dx,z+dz)]==ShowcaseBiome.Water)return false;
            return true;
        }
        private IEnumerable<int> Neighbors(int index)
        {
            int x=index%Size,z=index/Size;
            if(x>0)yield return index-1;if(x<Size-1)yield return index+1;if(z>0)yield return index-Size;if(z<Size-1)yield return index+Size;
        }
        private bool[] Reachable()
        {
            var seen=new bool[Tiles.Length];var queue=new Queue<int>();int start=Index(0,0);seen[start]=true;queue.Enqueue(start);
            while(queue.Count>0)foreach(int next in Neighbors(queue.Dequeue()))if(!seen[next]&&Tiles[next]!=ShowcaseBiome.Water){seen[next]=true;queue.Enqueue(next);}
            return seen;
        }
        private void CarveRoad(int targetX,int targetZ)
        {
            int start=Index(0,0),target=Index(targetX,targetZ);var cost=new double[Tiles.Length];var previous=new int[Tiles.Length];
            for(int i=0;i<cost.Length;i++){cost[i]=double.PositiveInfinity;previous[i]=-1;}cost[start]=0;
            var queue=new SortedSet<(double Cost,int Index)>();queue.Add((0,start));
            while(queue.Count>0)
            {
                var entry=queue.Min;queue.Remove(entry);int current=entry.Index;if(entry.Cost>cost[current])continue;if(current==target)break;
                foreach(int next in Neighbors(current))
                {
                    if(Tiles[next]==ShowcaseBiome.Water)continue;
                    int x=next%Size-Radius,z=next/Size-Radius;
                    double nextCost=cost[current]+(Roads[next]?.4:1+Noise(x*.16,z*.16)*3);
                    if(nextCost>=cost[next])continue;cost[next]=nextCost;previous[next]=current;queue.Add((nextCost,next));
                }
            }
            if(previous[target]<0)return;
            for(int node=target;node!=start;node=previous[node])
            {
                Roads[node]=true;Tiles[node]=ShowcaseBiome.Dirt;
                int x=node%Size-Radius,z=node/Size-Radius;
                // 중심 가까이는 3~5타일 폭의 주 길, 바깥은 1~3타일 폭의 가지 길.
                double width=x*x+z*z<Radius*Radius*.13?1.5+Noise(x*.15,z*.15):.65+Noise(x*.25,z*.25)*.8;
                for(int dz=-2;dz<=2;dz++)for(int dx=-2;dx<=2;dx++)
                {
                    if(dx*dx+dz*dz>width*width || !Inside(x+dx,z+dz))continue;
                    int adjacent=Index(x+dx,z+dz);if(Tiles[adjacent]==ShowcaseBiome.Water)continue;
                    Roads[adjacent]=true;Tiles[adjacent]=ShowcaseBiome.Dirt;
                }
            }
        }
    }
}
