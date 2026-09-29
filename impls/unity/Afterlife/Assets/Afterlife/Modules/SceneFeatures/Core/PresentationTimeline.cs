using System;
namespace Afterlife.Modules.SceneFeatures
{
    // 호스트 시간으로만 진행한다. 프리뷰/게임/필름 모두 동일한 접촉 이벤트를 얻는다.
    public sealed class ToolContactClock
    {
        public float Age { get; private set; }=10;
        private bool _contact;
        public void Begin(){Age=0;_contact=false;}
        public void Reset(){Age=10;_contact=true;}
        public bool Tick(float delta,float contactTime)
        {
            Age+=Math.Max(0,delta);
            if(_contact || Age<contactTime)return false;
            Age=contactTime;_contact=true;return true;
        }
    }
    public sealed class DayNightTransition
    {
        public float Blend { get; private set; }
        public void Set(bool night)=>Blend=night?1:0;
        public float Tick(float delta,bool night,float speed,float? forced=null)
        {
            if(forced.HasValue)Blend=Math.Max(0,Math.Min(1,forced.Value));
            else { float step=Math.Max(0,delta)*Math.Max(0,speed);Blend=night?Math.Min(1,Blend+step):Math.Max(0,Blend-step); }
            return Blend;
        }
    }
}
