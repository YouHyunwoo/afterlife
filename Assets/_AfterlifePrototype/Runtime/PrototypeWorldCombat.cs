using System;
using System.Collections.Generic;
using System.Linq;

namespace Afterlife.Prototype
{
    public sealed class Shot
    {
        public int Id, Target,Source;
        public Point Position, Direction,Start;
        public float Damage, Speed, Life=4;
        public bool Magic, Enemy,Arc,Lightning;
        public float Height=.65f,Elapsed,Duration,Peak,StartHeight=.65f;
    }
    public sealed partial class VillageModel
    {
        public readonly List<Shot> Projectiles=new List<Shot>();
        private int _nextShotId;
        private bool _obstaclesDirty=true;
        private readonly Dictionary<int,List<Body>> _obstacles=new Dictionary<int,List<Body>>();
        public static float EnemyVisionRadius(Kind kind)=>IsRanged(kind)?10:8;
        private Body EnemyTarget(Body enemy)
        {
            Body best=null;int bestTier=2;float bestDistance=float.MaxValue;
            foreach(var candidate in Bodies)
            {
                if(!candidate.Alive||(!candidate.Friendly&&!candidate.Building))continue;
                float distance=candidate.DistanceTo(enemy.Position);
                if(distance>EnemyVisionRadius(enemy.Kind))continue;
                int tier=candidate.Friendly||IsTower(candidate.Kind)||candidate.Kind==Kind.Wall?0:1;
                if(tier<bestTier||tier==bestTier&&distance<bestDistance)
                {best=candidate;bestTier=tier;bestDistance=distance;}
            }
            return best;
        }
        public static bool IsHostile(Kind kind)=>kind==Kind.Slime||kind>=Kind.Goblin&&kind<=Kind.Lizard;
        public static bool IsRanged(Kind kind)=>kind==Kind.Archer||kind==Kind.Mage||kind==Kind.GoblinShaman||kind==Kind.Lizard;
        public static bool IsMagic(Kind kind)=>kind==Kind.Mage||kind==Kind.GoblinShaman;
        public int WaveEnemyCount=>Wave==1?6:Wave==2?10:Wave==3?16:Wave==4?24:Wave==5?32:42;
        public int RemainingRaiders=>Bodies.Count(b=>b.Alive&&b.Raider)+(Phase==Phase.Night?Math.Max(0,WaveEnemyCount-_spawned):0);
        public string WaveThreat=>Wave==1?"슬라임 · 한 방향":Wave==2?"빠른 고블린 합류":Wave==3?"고블린 전사·주술사 · 양방향":Wave==4?"오크 돌격 + 주술사 지원":Wave==5?"리자드 궁수 · 세 방향":"총공세 · 중장갑과 원거리 혼성";
        private Kind WaveEnemy(int index)
        {
            if(Wave==1)return Kind.Slime;
            if(Wave==2)return index%3==0?Kind.Slime:Kind.Goblin;
            if(Wave==3)return index%4==0?Kind.GoblinShaman:index%4==1?Kind.GoblinWarrior:Kind.Goblin;
            if(Wave==4)return index%5==0?Kind.Orc:index%5==1?Kind.GoblinShaman:index%5==2?Kind.GoblinWarrior:Kind.Goblin;
            return index%5==0?Kind.GoblinShaman:index%5==1?Kind.Lizard:index%5==2?Kind.Orc:index%5==3?Kind.GoblinWarrior:Wave>=6?Kind.Orc:Kind.Goblin;
        }
        private static float EnemyHealth(Kind kind)=>kind==Kind.Orc?140:kind==Kind.GoblinWarrior?80:kind==Kind.GoblinShaman?55:kind==Kind.Lizard?65:kind==Kind.Goblin?36:30;
        private static float MoveSpeed(Kind kind)=>kind==Kind.Goblin?2.15f:kind==Kind.Lizard?2.2f:kind==Kind.Orc?.95f:kind==Kind.GoblinWarrior?1.45f:kind==Kind.GoblinShaman?1.3f:kind==Kind.Slime?1.2f:kind==Kind.Animal?.6f:2;
        private void EnemyHit(Body target,float damage)
        {
            if(target==null||!target.Alive||damage<=0)return;
            float absorbed=Math.Min(target.SpiritShield,damage);
            float actual=Math.Min(target.Health,Math.Max(0,damage-absorbed));
            if(absorbed>0)EmitText(target,$"보호 {absorbed:0.#}",FeedbackKind.Lightning);
            // 마지막 집이 사라져도 위치/시야가 남아 있는 피격 순간에 표시한다.
            if(actual>0)EmitText(target,$"-{actual:0.#}",FeedbackKind.EnemyDamage);ApplyDamage(target,damage);
        }
        private void ResolveAttack(Body source,Body target)
        {
            float amount=Damage(source.Kind)*(source.Raider?1+.18f*(Wave-1):1),baseDamage=amount;if(source.Friendly)amount=CriticalDamage(amount);
            if(!IsRanged(source.Kind)){if(!target.Building)Sound(amount>baseDamage?SoundCue.Critical:SoundCue.Hit,target.Position);if(source.Friendly)PlayerHit(target,amount);else EnemyHit(target,amount);return;}
            if(IsMagic(source.Kind))Sound(SoundCue.Magic,source.Position);
            Projectiles.Add(new Shot{Id=++_nextShotId,Source=source.Id,Target=target.Id,Start=source.Position,Position=source.Position,Direction=new Point(target.Position.X-source.Position.X,target.Position.Z-source.Position.Z),Damage=amount,Speed=IsMagic(source.Kind)?7:11,Magic=IsMagic(source.Kind),Enemy=source.Hostile});
        }
        private void ProcessProjectiles(float dt)
        {
            for(int i=Projectiles.Count-1;i>=0;i--)
            {
                var shot=Projectiles[i];var target=Find(shot.Target);shot.Life-=dt;
                if(shot.Life<=0){Projectiles.RemoveAt(i);continue;}
                if(shot.Lightning){if(target!=null)shot.Position=target.Position;continue;}
                if(target==null){Projectiles.RemoveAt(i);continue;}
                float previousHeight=shot.Height;
                shot.Direction=new Point(target.Position.X-shot.Position.X,target.Position.Z-shot.Position.Z);
                Point next;
                if(shot.Arc)
                {
                    shot.Elapsed+=dt;float t=Math.Min(1,shot.Elapsed/shot.Duration);
                    next=new Point(shot.Start.X+(target.Position.X-shot.Start.X)*t,shot.Start.Z+(target.Position.Z-shot.Start.Z)*t);
                    shot.Height=shot.StartHeight*(1-t)+.65f*t+4*shot.Peak*t*(1-t);
                }
                else{next=Point.Move(shot.Position,target.Position,shot.Speed*dt);float total=Math.Max(.1f,Point.Distance(shot.Start,target.Position));shot.Height=.65f+(shot.StartHeight-.65f)*Math.Min(1,Point.Distance(next,target.Position)/total);}
                if(!SightClear(shot.Position,next,shot.Source,target.Id,previousHeight,shot.Height)){Projectiles.RemoveAt(i);continue;}
                shot.Position=next;
                if((!shot.Arc||shot.Elapsed>=shot.Duration)&&target.DistanceTo(next)<=.12f){if(shot.Enemy)EnemyHit(target,shot.Damage);else PlayerHit(target,shot.Damage,!shot.Magic);Projectiles.RemoveAt(i);}
            }
        }
        private void SeedBiomes()
        {
            // 자원을 밀집시키되 4타일마다 통로를 남긴다. 시작 마을과 물은 피한다.
            for(int z=-Settings.MapRadius+2;z<Settings.MapRadius-1;z++)for(int x=-Settings.MapRadius+2;x<Settings.MapRadius-1;x++)
            {
                var terrain=Terrain.At(x,z);
                if(terrain!=TerrainKind.Forest&&terrain!=TerrainKind.Rocky||x%4==0||z%4==0)continue;
                if(_random.NextDouble()>(terrain==TerrainKind.Forest?.82:.72))continue;
                var p=new Point(x,z);if(!Walkable(p,.38f))continue;
                AddResource(terrain==TerrainKind.Forest?Kind.Tree:Kind.Rock,x,z);
            }
        }
        private static int ObstacleKey(int x,int z)=>(x+128)*256+z+128;
        private void EnsureObstacleIndex()
        {
            if(_obstaclesDirty)
            {
                _obstacles.Clear();
                foreach(var b in Bodies)
                {
                    if(!b.Alive||b.Unit||b.Kind==Kind.Treasure)continue;
                    float hx=b.Building?b.Width*.5f:.3f,hz=b.Building?b.Depth*.5f:.3f;
                    for(int z=(int)Math.Floor((b.Position.Z-hz)/2);z<=(int)Math.Floor((b.Position.Z+hz)/2);z++)
                    for(int x=(int)Math.Floor((b.Position.X-hx)/2);x<=(int)Math.Floor((b.Position.X+hx)/2);x++)
                    {
                        int key=ObstacleKey(x,z);if(!_obstacles.TryGetValue(key,out var list)){list=new List<Body>();_obstacles.Add(key,list);}list.Add(b);
                    }
                }
                _obstaclesDirty=false;
            }
        }
        private bool StaticFree(Point p,float radius)
        {
            EnsureObstacleIndex();
            for(int z=(int)Math.Floor((p.Z-radius)/2);z<=(int)Math.Floor((p.Z+radius)/2);z++)
            for(int x=(int)Math.Floor((p.X-radius)/2);x<=(int)Math.Floor((p.X+radius)/2);x++)
                if(_obstacles.TryGetValue(ObstacleKey(x,z),out var list))foreach(var b in list)if(b.Alive&&b.DistanceTo(p)<radius)return false;
            return true;
        }
    }
}
