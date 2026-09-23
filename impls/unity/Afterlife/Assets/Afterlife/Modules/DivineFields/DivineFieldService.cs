using System;
using System.Collections.Generic;

namespace Afterlife.Modules.DivineFields
{
    public enum FieldPlacementResult { Success, InvalidPosition, InsufficientPower, Overlap }

    public sealed class DivineField
    {
        public int Id { get; }
        public float X { get; }
        public float Z { get; }
        public float Radius { get; }
        public float Remaining { get; internal set; }
        internal float PulseClock;
        public float TimeUntilPulse(float interval) => Math.Max(0, interval - PulseClock);

        internal DivineField(int id, float x, float z, float radius, float duration)
        { Id = id; X = x; Z = z; Radius = radius; Remaining = duration; }
    }

    // 위치와 신력, 수명, 주기만 소유한다. 대상 판정과 실제 작업은 게임 어댑터가 처리한다.
    public sealed class DivineFieldService
    {
        private readonly List<DivineField> _fields = new List<DivineField>();
        private int _nextId;
        public IReadOnlyList<DivineField> Fields => _fields;
        public float MaximumPower { get; }
        public float PlacementCost { get; }
        public float RegenerationPerSecond { get; }
        public float Duration { get; }
        public float Power { get; private set; }
        public event Action<DivineField> Placed;
        public event Action<DivineField> Pulsed;
        public event Action<DivineField> Expired;

        public DivineFieldService(float maximumPower, float placementCost, float regenerationPerSecond, float duration)
        {
            if (!FinitePositive(maximumPower) || !FinitePositive(placementCost) || placementCost > maximumPower ||
                !FiniteNonnegative(regenerationPerSecond) || !FinitePositive(duration))
                throw new ArgumentOutOfRangeException(nameof(maximumPower), "Invalid divine field balance.");
            MaximumPower = maximumPower;
            PlacementCost = placementCost;
            RegenerationPerSecond = regenerationPerSecond;
            Duration = duration;
            Power = maximumPower;
        }

        public FieldPlacementResult TryPlace(float x, float z, float radius)
        {
            if (!Finite(x) || !Finite(z) || !FinitePositive(radius)) return FieldPlacementResult.InvalidPosition;
            if (Power + .0001f < PlacementCost) return FieldPlacementResult.InsufficientPower;
            foreach (var existing in _fields)
            {
                float dx = existing.X - x, dz = existing.Z - z;
                float minimum = Math.Min(existing.Radius, radius) * .5f;
                if (dx * dx + dz * dz < minimum * minimum) return FieldPlacementResult.Overlap;
            }
            Power = Math.Max(0, Power - PlacementCost);
            var field = new DivineField(++_nextId, x, z, radius, Duration);
            _fields.Add(field);
            Placed?.Invoke(field);
            return FieldPlacementResult.Success;
        }

        public void Tick(float deltaTime, float pulseInterval)
        {
            if (!FinitePositive(deltaTime) || !FinitePositive(pulseInterval)) return;
            Power = Math.Min(MaximumPower, Power + RegenerationPerSecond * deltaTime);
            // 이벤트 수신자가 배치를 요청해도 현재 프레임에 새 필드를 소급 처리하지 않는다.
            int count = _fields.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                var field = _fields[i];
                float activeTime = Math.Min(deltaTime, field.Remaining);
                field.PulseClock += activeTime;
                while (field.PulseClock + .00001f >= pulseInterval)
                {
                    field.PulseClock -= pulseInterval;
                    Pulsed?.Invoke(field);
                }
                field.Remaining = Math.Max(0, field.Remaining - deltaTime);
                if (field.Remaining > 0) continue;
                _fields.RemoveAt(i);
                Expired?.Invoke(field);
            }
        }

        public void Reset()
        {
            for (int i = _fields.Count - 1; i >= 0; i--) Expired?.Invoke(_fields[i]);
            _fields.Clear();
            _nextId = 0;
            Power = MaximumPower;
        }

        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool FinitePositive(float value) => Finite(value) && value > 0;
        private static bool FiniteNonnegative(float value) => Finite(value) && value >= 0;
    }
}
