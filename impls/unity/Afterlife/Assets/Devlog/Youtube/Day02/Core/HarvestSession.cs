using System;
namespace Afterlife.Devlog.Day02
{
    public enum HarvestMode { Click, Hold }
    public sealed class HarvestNode { public float X,Z; public int Remaining=48; }
    // Pure filming model. Main-game economy and Unity input are external.
    public sealed class HarvestSession
    {
        public readonly HarvestNode[] Nodes;
        public HarvestMode Mode { get; }
        public int Gold { get; private set; }
        public int LastAmount { get; private set; }
        public int WorkCount { get; private set; }
        public int WorkAmount { get; set; }=1;
        public float Interval { get; set; }=.7f;
        public event Action<int> Worked;
        private float _cooldown;
        public HarvestSession(HarvestNode[] nodes,HarvestMode mode){Nodes=nodes;Mode=mode;}
        public void Tick(float delta,int hovered,bool held,bool pressed)
        {
            _cooldown=Math.Max(0,_cooldown-Math.Max(0,delta));
            if(!(Mode==HarvestMode.Click?pressed:held)||_cooldown>0||hovered<0||hovered>=Nodes.Length||Nodes[hovered].Remaining<=0)return;
            LastAmount=Math.Min(Math.Max(1,WorkAmount),Nodes[hovered].Remaining);
            Nodes[hovered].Remaining-=LastAmount;Gold+=LastAmount;WorkCount++;
            // Releasing, switching targets and repeated clicking cannot bypass this cooldown.
            _cooldown=Math.Max(.05f,Interval);Worked?.Invoke(hovered);
        }
    }
}
