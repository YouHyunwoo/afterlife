using System;
using System.Collections.Generic;
using System.Linq;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private float _tacticsClock;private int _tacticsRevision=-1;
        private readonly Dictionary<int,int> _targetClaims=new Dictionary<int,int>();
        private void UpdateTactics(float dt)
        {
            _tacticsClock-=dt;if(_tacticsClock>0&&_tacticsRevision==TopologyRevision)return;
            _tacticsClock=Rules.Combat.TacticsSeconds;bool rebuild=_tacticsRevision!=TopologyRevision;_tacticsRevision=TopologyRevision;
            var army=Bodies.Where(b=>b.Alive&&b.Friendly).OrderBy(b=>b.Id).ToArray();
            var houses=Bodies.Where(b=>b.Alive&&b.Complete&&IsHousing(b.Kind)).ToArray();
            var threats=Bodies.Where(b=>b.Alive&&b.Hostile&&Visibility(b.Position)>.14f).ToArray();
            _targetClaims.Clear();int[] sectors={0,4,2,6,1,5,3,7};int automatic=0;
            // 서로 다른 방향을 먼저 채워 병영 출구 쪽으로만 순찰이 모이지 않게 한다.
            foreach(var unit in army)
            {
                if(!unit.ManualGuard)
                {
                    int slot=automatic++,sector=sectors[slot%8];
                    if(rebuild||!unit.GuardAssigned||unit.GuardSlot!=slot)
                    {unit.GuardSlot=slot;unit.GuardPost=FindGuardPost(sector,slot/8,houses,unit.Position);unit.GuardAssigned=true;unit.Route.Clear();unit.RouteTimer=0;unit.Destination=unit.GuardPost;}
                }
            }
            // 분산된 방어 위치를 기준으로 평가하고 이미 충분한 병력이 붙은 표적은 넘긴다.
            foreach(var unit in army.OrderBy(u=>threats.Length==0?0:threats.Min(t=>Point.Distance(u.GuardPost,t.Position))))
            {
                Body best=null;float score=float.MaxValue;
                foreach(var enemy in threats)
                {
                    float distance=Point.Distance(unit.Position,enemy.Position),home=Point.Distance(unit.GuardPost,enemy.Position);
                    if(unit.AttackMove?distance>Rules.Combat.AttackMoveAcquireRange:home>(unit.ManualGuard?Rules.Combat.ManualGuardRange:Rules.Combat.AutoGuardRange))continue;
                    int claims=_targetClaims.TryGetValue(enemy.Id,out int assigned)?assigned:0;
                    int capacity=Math.Min(4,Math.Max(1,(int)Math.Ceiling(enemy.Health/45)));
                    if(claims>=capacity&&distance>AttackRange(unit.Kind)+.35f)continue;
                    float value=distance+home*.65f+claims*5-(unit.CombatTarget==enemy.Id?1.2f:0);
                    if(value<score){score=value;best=enemy;}
                }
                int id=best?.Id??0;
                if(unit.CombatTarget!=id){unit.CombatTarget=id;unit.Route.Clear();unit.RouteTimer=0;unit.FiringTarget=0;}
                if(best!=null){_targetClaims[id]=(_targetClaims.TryGetValue(id,out int n)?n:0)+1;unit.MeleePoint=FindMeleePoint(unit,best);}
            }
        }
        private Point FindGuardPost(int sector,int row,Body[] houses,Point fallback)
        {
            if(houses.Length==0)return fallback;double angle=sector*Math.PI/4;
            float dx=(float)Math.Cos(angle),dz=(float)Math.Sin(angle);
            var house=houses.OrderByDescending(h=>h.Position.X*dx+h.Position.Z*dz).First();
            for(int i=0;i<32;i++)
            {
                double a=angle+(i%2==0?1:-1)*(i/2)*.075;float radius=3.1f+row*.8f+(i/12)*.5f;
                var p=new Point(house.Position.X+(float)Math.Cos(a)*radius,house.Position.Z+(float)Math.Sin(a)*radius);
                if(Walkable(p,.36f))return p;
            }
            return fallback;
        }
        private Point FindMeleePoint(Body unit,Body target)
        {
            float best=float.MaxValue;Point result=target.Position;double start=Math.Atan2(unit.Position.Z-target.Position.Z,unit.Position.X-target.Position.X);
            float radius=.32f+AttackRange(unit.Kind)*.9f;
            for(int i=0;i<16;i++)
            {
                double a=start+i*Math.PI/8;var point=new Point(target.Position.X+(float)Math.Cos(a)*radius,target.Position.Z+(float)Math.Sin(a)*radius);
                if(!Walkable(point,.34f)||!UnitFree(point,unit.Id))continue;
                float score=Point.Distance(unit.Position,point);
                foreach(var ally in Bodies)if(ally.Alive&&ally.Friendly&&ally.Id!=unit.Id&&ally.CombatTarget==target.Id&&Point.Distance(ally.MeleePoint,point)<.7f)score+=3;
                if(score<best){best=score;result=point;}
            }
            return result;
        }
        private Body FriendlyTarget(Body unit)
        {
            var target=Find(unit.CombatTarget);if(target==null)return null;
            if(Visibility(target.Position)<=.14f||(!unit.AttackMove&&Point.Distance(unit.GuardPost,target.Position)>(unit.ManualGuard?6.5f:8.5f)))return null;
            return target;
        }
        private void GuardPatrol(Body unit,float dt)
        {
            if(unit.CombatTarget!=0){unit.CombatTarget=0;unit.Route.Clear();unit.RouteTimer=0;unit.Destination=unit.GuardPost;}
            if(Point.Distance(unit.Position,unit.GuardPost)>1.3f)
            {
                if(Point.Distance(unit.Destination,unit.GuardPost)>.1f){unit.Destination=unit.GuardPost;unit.Route.Clear();unit.RouteTimer=0;}
                Navigate(unit,unit.GuardPost,null,.3f,dt);return;
            }
            unit.PatrolWait-=dt;if(unit.PatrolWait>0)return;
            if(Point.Distance(unit.Position,unit.Destination)<.25f){unit.PatrolWait=1.2f+(unit.Id%4)*.3f;double a=(unit.Id+unit.PatrolIndex++)*2.4;var point=new Point(unit.GuardPost.X+(float)Math.Cos(a)*.65f,unit.GuardPost.Z+(float)Math.Sin(a)*.65f);if(Walkable(point,.34f)){unit.Destination=point;unit.Route.Clear();unit.RouteTimer=0;}return;}
            Navigate(unit,unit.Destination,null,.2f,dt);
        }
    }
}
