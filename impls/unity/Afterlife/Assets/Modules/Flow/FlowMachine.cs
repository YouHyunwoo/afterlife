using System;
using System.Collections.Generic;

namespace Afterlife.Modules.Flow
{
    /// <summary>불변 상태 정의. 화면, 씬, 컨테이너에 의존하지 않는다.</summary>
    public sealed class FlowState<TState>
    {
        public TState Id { get; }
        public Action Enter { get; }
        public Action Exit { get; }

        public FlowState(TState id, Action enter = null, Action exit = null)
        {
            Id = id;
            Enter = enter;
            Exit = exit;
        }
    }

    public sealed class FlowTransition<TState, TTrigger>
    {
        public TState From { get; }
        public TTrigger Trigger { get; }
        public TState To { get; }
        public Func<bool> Guard { get; }

        public FlowTransition(TState from, TTrigger trigger, TState to, Func<bool> guard = null)
        {
            From = from;
            Trigger = trigger;
            To = to;
            Guard = guard;
        }
    }

    /// <summary>
    /// 단일 스레드 동기 흐름 머신. Exit → 상태 변경 → Enter → StateChanged 순서.
    /// 콜백 내 재진입 요청은 거절한다. 콜백 예외는 전파하고 Faulted로 정지한다.
    /// </summary>
    public sealed class FlowMachine<TState, TTrigger> : IDisposable
    {
        private readonly Dictionary<TState, FlowState<TState>> _states = new Dictionary<TState, FlowState<TState>>();
        private readonly Dictionary<TState, Dictionary<TTrigger, FlowTransition<TState, TTrigger>>> _transitions
            = new Dictionary<TState, Dictionary<TTrigger, FlowTransition<TState, TTrigger>>>();
        private TState _current;
        private bool _busy;
        private bool _disposed;

        public bool IsStarted { get; private set; }
        public bool IsFaulted { get; private set; }
        public bool IsTransitioning => _busy;
        public TState Current => IsStarted ? _current : throw new InvalidOperationException("흐름을 먼저 시작하세요.");
        public event Action<TState> StateChanged;

        public FlowMachine(IEnumerable<FlowState<TState>> states, IEnumerable<FlowTransition<TState, TTrigger>> transitions)
        {
            if (states == null) throw new ArgumentNullException(nameof(states));
            if (transitions == null) throw new ArgumentNullException(nameof(transitions));
            foreach (var state in states)
            {
                if (state == null || ReferenceEquals(state.Id, null)) throw new ArgumentException("상태와 ID는 null일 수 없습니다.", nameof(states));
                if (_states.ContainsKey(state.Id)) throw new ArgumentException("중복 상태 ID입니다.", nameof(states));
                _states.Add(state.Id, state);
                _transitions.Add(state.Id, new Dictionary<TTrigger, FlowTransition<TState, TTrigger>>());
            }
            if (_states.Count == 0) throw new ArgumentException("최소 한 상태가 필요합니다.", nameof(states));
            foreach (var transition in transitions)
            {
                if (transition == null || ReferenceEquals(transition.From, null) || ReferenceEquals(transition.To, null)
                    || ReferenceEquals(transition.Trigger, null)) throw new ArgumentException("전환 정의에 null이 있습니다.", nameof(transitions));
                if (!_states.ContainsKey(transition.From) || !_states.ContainsKey(transition.To))
                    throw new ArgumentException("등록하지 않은 상태로의 전환입니다.", nameof(transitions));
                var routes = _transitions[transition.From];
                if (routes.ContainsKey(transition.Trigger)) throw new ArgumentException("같은 상태/이벤트의 전환이 중복됩니다.", nameof(transitions));
                routes.Add(transition.Trigger, transition);
            }
        }

        public void Start(TState initialState)
        {
            ThrowIfDisposed();
            if (IsStarted || _busy || IsFaulted) throw new InvalidOperationException("흐름은 한 번만 시작할 수 있습니다.");
            if (ReferenceEquals(initialState, null) || !_states.ContainsKey(initialState)) throw new ArgumentException("등록하지 않은 시작 상태입니다.", nameof(initialState));
            Execute(() =>
            {
                _current = initialState;
                IsStarted = true;
                _states[_current].Enter?.Invoke();
                StateChanged?.Invoke(_current);
            });
        }

        public bool TrySend(TTrigger trigger)
        {
            ThrowIfDisposed();
            if (!IsStarted || IsFaulted || _busy || ReferenceEquals(trigger, null)) return false;
            if (!_transitions[_current].TryGetValue(trigger, out var transition)) return false;
            bool changed = false;
            Execute(() =>
            {
                if (transition.Guard != null && !transition.Guard()) return;
                _states[_current].Exit?.Invoke();
                _current = transition.To;
                _states[_current].Enter?.Invoke();
                StateChanged?.Invoke(_current);
                changed = true;
            });
            return changed;
        }

        private void Execute(Action action)
        {
            _busy = true;
            try { action(); }
            catch { IsFaulted = true; throw; }
            finally { _busy = false; }
        }

        public void Dispose()
        {
            if (_disposed) return;
            if (_busy) throw new InvalidOperationException("전환 중에는 해제할 수 없습니다.");
            // 정리 실패 여부와 관계없이 Dispose는 한 번만 수행한다.
            _disposed = true;
            try { if (IsStarted && !IsFaulted) Execute(() => _states[_current].Exit?.Invoke()); }
            finally
            {
                StateChanged = null;
                _states.Clear();
                _transitions.Clear();
                IsStarted = false;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(FlowMachine<TState, TTrigger>));
        }
    }
}
