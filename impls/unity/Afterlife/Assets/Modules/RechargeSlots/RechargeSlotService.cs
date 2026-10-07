using System;
using System.Collections.Generic;

namespace Afterlife.Modules.RechargeSlots
{
    public sealed class RechargeSlot
    {
        public float Remaining { get; internal set; }
        public bool Ready => Remaining <= 0;
    }

    /// <summary>Independent, parallel charges. The caller owns gameplay time and placement validation.</summary>
    public sealed class RechargeSlotService
    {
        private readonly List<RechargeSlot> _slots = new List<RechargeSlot>();
        public IReadOnlyList<RechargeSlot> Slots { get; }
        public float RechargeSeconds { get; private set; }
        public bool HasReady { get { foreach (var slot in _slots) if (slot.Ready) return true; return false; } }
        public event Action Changed;
        public event Action<int> Recharged;
        public event Action ResetPerformed;

        public RechargeSlotService(int capacity, float seconds)
        {
            Slots = _slots.AsReadOnly();
            Configure(capacity, seconds);
        }

        public void Configure(int capacity, float seconds)
        {
            if (capacity < 1 || seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(capacity));
            bool changed = capacity != _slots.Count || seconds != RechargeSeconds;
            if (!changed) return;
            if (RechargeSeconds > 0)
                foreach (var slot in _slots) slot.Remaining = slot.Remaining / RechargeSeconds * seconds;
            RechargeSeconds = seconds;
            while (_slots.Count < capacity) _slots.Add(new RechargeSlot());
            if (_slots.Count > capacity) _slots.RemoveRange(capacity, _slots.Count - capacity);
            Changed?.Invoke();
        }

        public bool TryConsume()
        {
            foreach (var slot in _slots)
            {
                if (!slot.Ready) continue;
                slot.Remaining = RechargeSeconds;
                Changed?.Invoke();
                return true;
            }
            return false;
        }

        public void Tick(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            bool changed = false;
            for (int i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                if (slot.Ready) continue;
                slot.Remaining = Math.Max(0, slot.Remaining - seconds);
                changed = true;
                if (slot.Ready) Recharged?.Invoke(i);
            }
            if (changed) Changed?.Invoke();
        }

        public void Reset()
        {
            foreach (var slot in _slots) slot.Remaining = 0;
            ResetPerformed?.Invoke();
            Changed?.Invoke();
        }
    }
}
