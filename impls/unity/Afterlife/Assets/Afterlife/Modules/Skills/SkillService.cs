using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Afterlife.Modules.Skills
{
    public sealed class SkillDefinition
    {
        public string Id { get; }
        public float Cooldown { get; }
        public float Duration { get; }
        public float PulseInterval { get; }
        public SkillDefinition(string id, float cooldown, float duration = 0, float pulseInterval = 0)
        {
            if (string.IsNullOrWhiteSpace(id) || !Valid(cooldown) || !Valid(duration) || !Valid(pulseInterval)) throw new ArgumentException("유효하지 않은 스킬 정의입니다.");
            Id = id; Cooldown = cooldown; Duration = duration; PulseInterval = pulseInterval;
        }
        private static bool Valid(float value) => value >= 0 && !float.IsNaN(value) && !float.IsInfinity(value);
    }
    public sealed class SkillState
    {
        public int Level { get; internal set; }
        public float CooldownRemaining { get; internal set; }
        public float ActiveRemaining { get; internal set; }
        internal float PulseClock;
    }
    public interface ISkillActivationPolicy { bool CanActivate(string id); }

    // 강화 시스템 참조 없음. 해금은 외부 조립 계층이 SetLevel로 전달한다.
    public sealed class SkillService
    {
        private readonly Dictionary<string, SkillDefinition> _definitions;
        private readonly Dictionary<string, SkillState> _states;
        private readonly ISkillActivationPolicy _policy;
        public IReadOnlyDictionary<string, SkillState> States { get; }
        public event Action<string> Activated;
        public event Action<string> Pulsed;
        public event Action<string> Expired;
        public SkillService(IEnumerable<SkillDefinition> definitions, ISkillActivationPolicy policy)
        {
            _definitions = definitions.ToDictionary(item => item.Id, StringComparer.Ordinal);
            _states = _definitions.Keys.ToDictionary(id => id, _ => new SkillState());
            States = new ReadOnlyDictionary<string, SkillState>(_states);
            _policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }
        public void SetLevel(string id, int level)
        {
            var state = _states[id]; state.Level = Math.Max(0, level);
            if (state.Level == 0) state.ActiveRemaining = state.CooldownRemaining = state.PulseClock = 0;
        }
        public bool TryActivate(string id)
        {
            if (!_states.TryGetValue(id, out var state) || state.Level == 0 || state.CooldownRemaining > 0 || state.ActiveRemaining > 0 || !_policy.CanActivate(id)) return false;
            var definition = _definitions[id];
            state.CooldownRemaining = definition.Cooldown; state.ActiveRemaining = definition.Duration; state.PulseClock = 0;
            Activated?.Invoke(id); return true;
        }
        public void ReduceCooldown(string id, float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds < 0) return;
            if (_states.TryGetValue(id, out var state)) state.CooldownRemaining = Math.Max(0, state.CooldownRemaining - seconds);
        }
        public void Tick(float seconds)
        {
            if (seconds <= 0 || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            foreach (var pair in _states)
            {
                var state = pair.Value; var definition = _definitions[pair.Key];
                state.CooldownRemaining = Math.Max(0, state.CooldownRemaining - seconds);
                if (state.ActiveRemaining <= 0) continue;
                state.PulseClock += Math.Min(seconds, state.ActiveRemaining);
                state.ActiveRemaining = Math.Max(0, state.ActiveRemaining - seconds);
                if (definition.PulseInterval > 0)
                    while (state.PulseClock + .00001f >= definition.PulseInterval)
                    { state.PulseClock -= definition.PulseInterval; Pulsed?.Invoke(pair.Key); }
                if (state.ActiveRemaining <= 0) Expired?.Invoke(pair.Key);
            }
        }
        public void Reset()
        {
            foreach (var state in _states.Values) { state.Level = 0; state.ActiveRemaining = state.CooldownRemaining = state.PulseClock = 0; }
        }
    }
}
