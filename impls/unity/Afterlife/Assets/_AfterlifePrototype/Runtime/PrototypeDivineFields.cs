using Afterlife.Modules.DivineFields;

namespace Afterlife.Prototype
{
    // 필드 코어와 기존 채집/건설/수리/전투 계산을 연결하는 게임 어댑터.
    public sealed partial class VillageModel
    {
        public DivineFieldService DivineFields { get; private set; }
        public float DivinePower => DivineFields.Power;
        public float DivineFieldRadius => Radius * 1.8f;
        private Point? _activeDivineFieldCenter;
        private float _activeDivineFieldRadius;

        private void InitializeDivineFields()
        {
            DivineFields = new DivineFieldService(Settings.DivinePowerMaximum, Settings.DivineFieldCost,
                Settings.DivinePowerRegeneration, Settings.DivineFieldDuration);
            DivineFields.Pulsed += OnDivineFieldPulse;
        }

        public FieldPlacementResult PlaceDivineField(Point center)
        {
            if (Frozen || Mode != InputMode.Work || float.IsNaN(center.X) || float.IsNaN(center.Z) ||
                float.IsInfinity(center.X) || float.IsInfinity(center.Z) ||
                System.Math.Abs(center.X) >= Settings.MapRadius || System.Math.Abs(center.Z) >= Settings.MapRadius ||
                Terrain.At(center) == TerrainKind.Water || Visibility(center) <= .12f)
                return FieldPlacementResult.InvalidPosition;

            var result = DivineFields.TryPlace(center.X, center.Z, DivineFieldRadius);
            if (result == FieldPlacementResult.Success) Say($"신력 {DivineFields.PlacementCost:0} 투자 · {DivineFields.Duration:0.#}초 동안 자동 작업");
            else if (result == FieldPlacementResult.InsufficientPower) { Sound(SoundCue.Error); Say("신력이 부족합니다."); }
            else if (result == FieldPlacementResult.Overlap) { Sound(SoundCue.Error); Say("기존 필드와 너무 가깝습니다."); }
            return result;
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
            return !Frozen && Mode == InputMode.Work && body != null && Visibility(body.Position) > .12f &&
                (body.MovingOut || body.MoveSource != 0 || body.Kind == Kind.Tree || body.Kind == Kind.Rock ||
                 body.Kind == Kind.Chest || body.Kind == Kind.Treasure || body.Hostile || body.Kind == Kind.Animal ||
                 body.Building && (!body.Complete || body.Health < body.MaxHealth && Money > 0));
        }
    }
}
