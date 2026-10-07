using Afterlife.Modules.DivineFields;
using Afterlife.Modules.RechargeSlots;

namespace Afterlife.Prototype
{
    // 필드 코어와 기존 채집/건설/수리/전투 계산을 연결하는 게임 어댑터.
    public sealed partial class VillageModel
    {
        public DivineFieldService DivineFields { get; private set; }
        public RechargeSlotService FieldSlots { get; private set; }
        public bool CanPlaceField => Settings.MvpSlots ? FieldSlots.HasReady : DivinePower >= DivineFields.PlacementCost;
        public float FieldRadiusReward => Settings.MvpSlots ? Rules.Relics.FieldRadius * RelicLevel(RelicKind.FieldRadius) : 0;
        public float SlotReduction => Settings.MvpSlots ? System.Math.Min(.5f, Effect("slotRecharge") + Rules.Relics.SlotRecharge * RelicLevel(RelicKind.SlotRecharge)) : 0;

        private void RefreshFieldSlots()
        {
            if (FieldSlots == null) return;
            int capacity = System.Math.Min(Settings.MaximumSlots, Settings.InitialSlots + (int)Effect("fieldSlot") + RelicLevel(RelicKind.FieldSlot));
            FieldSlots.Configure(capacity, Settings.SlotRechargeSeconds * (1 - SlotReduction));
        }
        private void ResetFieldSlots() { RefreshFieldSlots(); FieldSlots?.Reset(); }
        public float DivinePower => DivineFields.Power;
        public float DivineFieldRadius => Radius * 1.8f;
        private Point? _activeDivineFieldCenter;
        private float _activeDivineFieldRadius;

        private void InitializeDivineFields()
        {
            if (Settings.InitialSlots < 1 || Settings.MaximumSlots < Settings.InitialSlots || Settings.MaximumSlots > 3)
                throw new System.ArgumentException("필드 슬롯 수는 1~3개여야 합니다.");
            FieldSlots = new RechargeSlotService(Settings.InitialSlots, Settings.SlotRechargeSeconds);
            DivineFields = new DivineFieldService(Settings.DivinePowerMaximum, Settings.DivineFieldCost,
                Settings.DivinePowerRegeneration, Settings.DivineFieldDuration);
            DivineFields.Pulsed += OnDivineFieldPulse;
        }

        public FieldPlacementResult PlaceDivineField(Point center)
        {
            return PlaceDivineField(center, 0);
        }

        public FieldPlacementResult PlaceDivineFieldOnTarget(int id)
        {
            var target = Find(id);
            return target != null && target.Alive && Visibility(target.Position) > .12f
                ? PlaceDivineField(target.Position, id) : FieldPlacementResult.InvalidPosition;
        }

        private FieldPlacementResult PlaceDivineField(Point center, int targetId)
        {
            if (Frozen || Mode != InputMode.Work || float.IsNaN(center.X) || float.IsNaN(center.Z) ||
                float.IsInfinity(center.X) || float.IsInfinity(center.Z) ||
                System.Math.Abs(center.X) >= Settings.MapRadius || System.Math.Abs(center.Z) >= Settings.MapRadius ||
                Terrain.At(center) == TerrainKind.Water || Visibility(center) <= .12f)
                return FieldPlacementResult.InvalidPosition;

            if (Settings.MvpSlots && !FieldSlots.HasReady) { Sound(SoundCue.Error); Say("필드 슬롯이 충전 중입니다."); return FieldPlacementResult.NoReadySlot; }
            var result = DivineFields.TryPlace(center.X, center.Z, DivineFieldRadius, targetId, !Settings.MvpSlots);
            if (result == FieldPlacementResult.Success)
            {
                if (Settings.MvpSlots) FieldSlots.TryConsume();
                Say(Settings.MvpSlots ? $"슬롯 사용 · {DivineFields.Duration:0.#}초 동안 자동 작업" : $"영력 {DivineFields.PlacementCost:0} 투자 · {DivineFields.Duration:0.#}초 동안 자동 작업");
            }
            else if (result == FieldPlacementResult.InsufficientPower) { Sound(SoundCue.Error); Say("영력이 부족합니다."); }
            else if (result == FieldPlacementResult.Overlap) { Sound(SoundCue.Error); Say("기존 필드와 너무 가깝습니다."); }
            return result;
        }

        private void UpdateDivineFieldTargets()
        {
            foreach (var field in DivineFields.Fields)
            {
                if (field.TargetId == 0) continue;
                var target = Find(field.TargetId);
                if (target != null && target.Alive)
                    DivineFields.MoveTarget(field.TargetId, target.Position.X, target.Position.Z);
            }
        }

        private void OnDivineFieldPulse(DivineField field)
        {
            if (Frozen) return;
            bool changed = false;
            var center = new Point(field.X, field.Z);
            _activeDivineFieldCenter = center;
            _activeDivineFieldRadius = field.Radius;
            try
            {
                foreach (var body in Bodies)
                    if (body.Alive && body.DistanceTo(center) <= field.Radius)
                        ContributeBody(body, ref changed);
            }
            finally { _activeDivineFieldCenter = null; _activeDivineFieldRadius = 0; }
            PulseCount++;
            if (changed) { TopologyRevision++; Recalculate(); }
        }

        public bool CanDivineFieldInteract(int id)
        {
            var body = Find(id);
            return !Frozen && Mode == InputMode.Work && body != null && body.Alive && Visibility(body.Position) > .12f;
        }
    }
}
