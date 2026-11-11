using Afterlife.Modules.HoldInteraction;

namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        // 입력·대상 판정과 기존 효과를 독립 충전 코어에 연결한다.
        private readonly InteractionCharge _manualWorkCharge = new InteractionCharge(1f);
        private int _manualWorkTarget;
        private bool _manualWorkHeld;
        private bool _legacyFieldInteraction;

        public bool UsesHoldInteraction => !_legacyFieldInteraction;
        public float ManualWorkInterval => _manualWorkCharge.IntervalSeconds;
        public float ManualWorkFraction => _manualWorkCharge.Fraction;
        public float ManualWorkCooldownRemaining => _manualWorkCharge.RemainingSeconds;
        public int ManualWorkTarget => _manualWorkHeld ? _manualWorkTarget : 0;

        public void UpdateManualWorkInput(int id, bool held)
        {
            _manualWorkHeld = held && UsesHoldInteraction && !Frozen && Mode == InputMode.Work;
            _manualWorkTarget = _manualWorkHeld ? id : 0;
            TryManualWork();
        }
        public void CancelManualWork()
        {
            _manualWorkHeld = false;
            _manualWorkTarget = 0;
        }
        // 촬영용 기존 필드 소비자만 명시적으로 선택한다. 일반 플레이 복귀 시 필드를 비운다.
        public void SetLegacyFieldInteraction(bool enabled)
        {
            CancelManualWork();
            _legacyFieldInteraction = enabled;
            if (!enabled) DivineFields.Reset();
        }
        private void ResetManualWork()
        {
            CancelManualWork();
            _manualWorkCharge.Reset(1f);
        }
        public bool CanManualWork(int id)
        {
            var body = Find(id);
            return UsesHoldInteraction && !Frozen && Mode == InputMode.Work && body != null && body.Alive
                && Visibility(body.Position) > .12f
                && (body.MovingOut || body.MoveSource != 0 || body.Kind == Kind.Tree || body.Kind == Kind.Rock
                    || body.Kind == Kind.Chest || body.Kind == Kind.Treasure || body.Hostile || body.Kind == Kind.Animal
                    || body.Building && (!body.Complete || body.Health < body.MaxHealth && Money > 0));
        }
        private void TickManualWork(float deltaSeconds)
        {
            if (!UsesHoldInteraction) return;
            _manualWorkCharge.Advance(deltaSeconds);
            TryManualWork();
        }
        private bool TryManualWork()
        {
            if (!_manualWorkHeld || !CanManualWork(_manualWorkTarget) || !_manualWorkCharge.TryConsume(true)) return false;
            bool changed = false;
            ContributeBody(Find(_manualWorkTarget), ref changed);
            PulseCount++;
            if (changed) { TopologyRevision++; Recalculate(); }
            return true;
        }
    }
}
