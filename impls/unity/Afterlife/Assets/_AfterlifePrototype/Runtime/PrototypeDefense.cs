using System;
using System.Linq;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        // Unity raycast 결과를 주입받되 모델만 사용할 때도 같은 장애물 판정으로 동작한다.
        public Func<Body,Body,bool> ArrowRayBlocked;
        public float TowerRange(Kind kind)=>Building(kind).AttackRange+(ActiveRuins?Rules.Economy.RuinsRangeBonus:0)+Rules.Growth.TowerResearchRangePerLevel*Level(kind==Kind.MagicTower?Upgrade.MagicRange:Upgrade.ArrowRange);
        public static bool Obstacle(Body body)=>body.Building&&body.BlocksMovement||body.Kind==Kind.Tree||body.Kind==Kind.Rock||body.Kind==Kind.Chest||body.Kind==Kind.AbandonedAltar||body.Kind==Kind.Ruins;
        public static float ObstacleHeight(Body body)=>body.Kind==Kind.Tree?1.8f:body.Kind==Kind.Rock?.65f:body.Kind==Kind.Wall?1.25f:body.Kind==Kind.ArrowTower||body.Kind==Kind.MagicTower?2.5f:body.Building?1.6f:1.2f;
        private bool SightClear(Point from,Point to,int sourceId,int targetId,float startHeight,float endHeight)
        {
            EnsureObstacleIndex();int steps=Math.Max(1,(int)(Point.Distance(from,to)*12));
            for(int i=1;i<=steps;i++)
            {
                float t=i/(float)steps;var point=new Point(from.X+(to.X-from.X)*t,from.Z+(to.Z-from.Z)*t);float height=startHeight+(endHeight-startHeight)*t;
                for(int z=(int)Math.Floor((point.Z-.04f)/2);z<=(int)Math.Floor((point.Z+.04f)/2);z++)
                for(int x=(int)Math.Floor((point.X-.04f)/2);x<=(int)Math.Floor((point.X+.04f)/2);x++)
                    if(_obstacles.TryGetValue(ObstacleKey(x,z),out var bucket))foreach(var body in bucket)
                        if(body.Alive&&body.Id!=sourceId&&body.Id!=targetId&&Obstacle(body)&&body.DistanceTo(point)<.04f&&height<ObstacleHeight(body))return false;
            }return true;
        }
        private void SeekFiringPosition(Body unit,Body target,float range,float dt)
        {
            // 목표 중심까지 걸어가며 막히는 대신 공격 가능한 원주 지점을 탐색한다.
            unit.FiringSearch-=dt;
            if(unit.FiringTarget!=target.Id||unit.FiringSearch<=0)
            {
                unit.FiringTarget=target.Id;unit.FiringSearch=.8f;Point? best=null;float score=float.MaxValue;
                for(int ring=0;ring<3;ring++)for(int i=0;i<16;i++)
                {
                    double a=i*Math.PI/8;float r=range*(.9f-ring*.2f);
                    var p=new Point(target.Position.X+(float)Math.Cos(a)*r,target.Position.Z+(float)Math.Sin(a)*r);
                    float d=Point.Distance(unit.Position,p);
                    if(d<score&&Walkable(p,.34f)&&UnitFree(p,unit.Id)&&ClearAttack(p,target,unit.Id)){best=p;score=d;}
                }
                Point firingPosition=best??Point.Move(target.Position,unit.Position,range*.7f+.4f);
                // 같은 사격 위치를 다시 선택해도 진행 중인 경로를 버리지 않는다.
                if(firingPosition.X!=unit.FiringPosition.X||firingPosition.Z!=unit.FiringPosition.Z)
                {unit.FiringPosition=firingPosition;unit.Route.Clear();unit.RouteTimer=0;}
            }
            Navigate(unit,unit.FiringPosition,null,.25f,dt);
        }
        private float ArcPeak(Body source,Body target)
        {
            float peak=2;int steps=Math.Max(2,(int)(Point.Distance(source.Position,target.Position)*10));
            for(int i=1;i<steps;i++)
            {
                float t=i/(float)steps;var p=new Point(source.Position.X+(target.Position.X-source.Position.X)*t,source.Position.Z+(target.Position.Z-source.Position.Z)*t);
                foreach(var body in Bodies)if(body.Alive&&body!=source&&body!=target&&Obstacle(body)&&body.DistanceTo(p)<.08f)
                    peak=Math.Max(peak,(ObstacleHeight(body)+.4f-(2.4f*(1-t)+.65f*t))/(4*t*(1-t)));
            }return Math.Min(30,peak);
        }
        private void ProcessTowers(float dt)
        {
            foreach(var tower in Bodies)
            {
                if(!tower.Alive||!tower.Complete||tower.MovingOut||tower.Kind!=Kind.ArrowTower&&tower.Kind!=Kind.MagicTower)continue;
                tower.AttackTimer-=dt;if(tower.AttackTimer>0)continue;
                var enemies=Bodies.Where(b=>b.Alive&&b.Hostile&&Point.Distance(tower.Position,b.Position)<=TowerRange(tower)&&Visibility(b.Position)>.1f).OrderBy(b=>Point.Distance(tower.Position,b.Position)).Take(TargetCount).ToArray();
                if(enemies.Length==0)continue;
                tower.AttackTimer=Building(tower.Kind).AttackSeconds;
                tower.Facing=new Point(enemies[0].Position.X-tower.Position.X,enemies[0].Position.Z-tower.Position.Z);
                if(tower.Kind==Kind.MagicTower)Sound(SoundCue.Magic,tower.Position);
                foreach(var enemy in enemies)
                {
                    if(tower.Kind==Kind.MagicTower)
                    {
                        PlayerHit(enemy,CriticalDamage(TowerDamage(tower)),false);
                        Projectiles.Add(new Shot{Id=++_nextShotId,Source=tower.Id,Target=enemy.Id,Start=tower.Position,Position=enemy.Position,Lightning=true,Life=.20f,Height=2.4f});
                    }
                    else
                    {
                        bool arc=(ArrowRayBlocked!=null&&ArrowRayBlocked(tower,enemy))||!SightClear(tower.Position,enemy.Position,tower.Id,enemy.Id,2.4f,.65f);
                        var direction=new Point(enemy.Position.X-tower.Position.X,enemy.Position.Z-tower.Position.Z);
                        Projectiles.Add(new Shot{Id=++_nextShotId,Source=tower.Id,Target=enemy.Id,Start=tower.Position,Position=tower.Position,Direction=direction,Damage=CriticalDamage(TowerDamage(tower)),Speed=Building(tower.Kind).ProjectileSpeed,Arc=arc,StartHeight=2.4f,Height=2.4f,Peak=arc?ArcPeak(tower,enemy):0,Duration=Math.Max(.6f,Point.Distance(tower.Position,enemy.Position)/7),Life=5});
                    }
                }
            }
        }
    }
}
