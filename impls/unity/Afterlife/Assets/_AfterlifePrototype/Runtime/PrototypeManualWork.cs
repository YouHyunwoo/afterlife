namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private float _manualWorkElapsed=float.MaxValue;
        private int _manualWorkTarget;
        public float ManualWorkInterval=>Interval*.5f;
        public float ManualWorkCooldownRemaining=>System.Math.Max(0,ManualWorkInterval-_manualWorkElapsed);
        public bool TryBeginManualWork(int id)
        {
            if(_manualWorkTarget!=0 || ManualWorkCooldownRemaining>0 || !CanManualWork(id))return false;
            _manualWorkTarget=id;_manualWorkElapsed=0;
            return true;
        }
        public void CancelManualWork()
        {
            if(_manualWorkTarget==0)return;
            _manualWorkTarget=0;_manualWorkElapsed=float.MaxValue;
        }
        private void ResetManualWork(){_manualWorkTarget=0;_manualWorkElapsed=float.MaxValue;}
        public bool CanManualWork(int id)
        {
            var b=Find(id);
            return !CursorAutomationUnlocked && !Frozen && !Ended && Mode==InputMode.Work && b!=null && b.Alive
                && Visibility(b.Position)>.12f && (b.MovingOut || b.MoveSource!=0 || b.Kind==Kind.Tree || b.Kind==Kind.Rock
                || b.Kind==Kind.Chest || b.Kind==Kind.Treasure || b.Hostile || b.Kind==Kind.Animal
                || b.Building && (!b.Complete || b.Health<b.MaxHealth && Money>0));
        }
        public bool ManualWork(int id)
        {
            if(_manualWorkTarget!=id || !CanManualWork(id)){CancelManualWork();return false;}
            _manualWorkTarget=0;
            bool changed=false;ContributeBody(Find(id),ref changed);PulseCount++;
            if(changed){TopologyRevision++;Recalculate();}
            return true;
        }
    }
}
