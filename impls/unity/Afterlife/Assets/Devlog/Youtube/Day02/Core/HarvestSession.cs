using System;
using Afterlife.Modules.DivineFields;
namespace Afterlife.Devlog.Day02
{
    public enum HarvestMode { Click, Hover, Field }
    public sealed class HarvestNode
    {
        public float X, Z;
        public int Remaining = 16;
    }
    // 촬영용 과거 규칙 재현. 본편 진행/경제 모델과 분리한다.
    public sealed class HarvestSession
    {
        public readonly HarvestNode[] Nodes;
        public readonly DivineFieldService Fields = new DivineFieldService(100,30,3,8);
        public HarvestMode Mode { get; }
        public int Gold { get; private set; }
        public event Action<int> Worked;
        private float _hoverClock;
        private int _previousHover=-1;
        public HarvestSession(HarvestNode[] nodes, HarvestMode mode)
        {
            Nodes=nodes; Mode=mode;
            Fields.Pulsed += field => {
                for(int i=0;i<Nodes.Length;i++) {
                    float x=Nodes[i].X-field.X,z=Nodes[i].Z-field.Z;
                    if(x*x+z*z<=field.Radius*field.Radius) Work(i);
                }
            };
        }
        public void Click(int index,float x,float z)
        {
            if(Mode==HarvestMode.Click) Work(index);
            else if(Mode==HarvestMode.Field) Fields.TryPlace(x,z,1.1f);
        }
        public void Tick(float delta,int hovered)
        {
            if(delta<=0)return;
            Fields.Tick(delta,.7f);
            if(Mode!=HarvestMode.Hover)return;
            if(hovered<0 || hovered>=Nodes.Length) {_hoverClock=0;_previousHover=-1;return;}
            if(_previousHover!=hovered) {_hoverClock=0;_previousHover=hovered;}
            _hoverClock+=delta;
            while(_hoverClock>=.7f){_hoverClock-=.7f;Work(hovered);}
        }
        private void Work(int index)
        {
            if(index<0 || index>=Nodes.Length || Nodes[index].Remaining<=0)return;
            Nodes[index].Remaining--;Gold+=5;Worked?.Invoke(index);
        }
    }
}
