using System;
using System.Collections.Generic;
using Afterlife.Modules.Flow;

namespace Afterlife.Modules.Threshold
{
    public enum ThresholdScreen { Title, Lobby, Playing, GameOver, Clear }
    public enum ThresholdAction { NewGame, EnterTrial, Fail, Complete, Lobby, Title, OpenModal, CloseModal, Quit }

    /// <summary>Camera/UI independent session. A pending transition locks input until its duration has elapsed.</summary>
    public sealed class ThresholdSession : IDisposable
    {
        private readonly FlowMachine<ThresholdScreen, ThresholdScreen> _flow;
        private readonly List<FlowTransition<ThresholdScreen, ThresholdScreen>> _routes;
        private float _duration;
        private float _elapsed;
        public ThresholdScreen Current => _flow.Current;
        public ThresholdScreen From { get; private set; }
        public ThresholdScreen To { get; private set; }
        public bool IsMoving { get; private set; }
        public bool ModalOpen { get; private set; }
        public float Progress => IsMoving ? Math.Min(1f, _elapsed / _duration) : 1f;
        public bool IsCameraTravel => IsVoidTravel(From, To);
        public bool IsResultEntry => From == ThresholdScreen.Playing && IsResult(To);
        public static bool IsResult(ThresholdScreen screen) => screen == ThresholdScreen.GameOver || screen == ThresholdScreen.Clear;
        public static bool IsVoidTravel(ThresholdScreen from, ThresholdScreen to) =>
            (from == ThresholdScreen.Title && to == ThresholdScreen.Lobby) || (from == ThresholdScreen.Lobby && to == ThresholdScreen.Title);
        public static ThresholdScreen BackgroundFor(ThresholdScreen screen) => IsResult(screen) ? ThresholdScreen.Playing : screen;
        public ThresholdScreen Presented => IsMoving && !IsCameraTravel && Progress >= .48f ? To : Current;
        public bool CanPlay => Current == ThresholdScreen.Playing && !IsMoving && !ModalOpen;
        public event Action<ThresholdScreen> Entered;
        public event Action<bool> InteractionChanged;

        public ThresholdSession()
        {
            var states = new List<FlowState<ThresholdScreen>>();
            foreach (ThresholdScreen id in Enum.GetValues(typeof(ThresholdScreen))) states.Add(new FlowState<ThresholdScreen>(id));
            var routes = new List<FlowTransition<ThresholdScreen, ThresholdScreen>>();
            Add(routes, ThresholdScreen.Title, ThresholdScreen.Lobby);
            Add(routes, ThresholdScreen.Lobby, ThresholdScreen.Playing, ThresholdScreen.Title);
            Add(routes, ThresholdScreen.Playing, ThresholdScreen.GameOver, ThresholdScreen.Clear, ThresholdScreen.Lobby);
            Add(routes, ThresholdScreen.GameOver, ThresholdScreen.Lobby, ThresholdScreen.Playing, ThresholdScreen.Title);
            Add(routes, ThresholdScreen.Clear, ThresholdScreen.Lobby, ThresholdScreen.Playing, ThresholdScreen.Title);
            _routes = routes;
            _flow = new FlowMachine<ThresholdScreen, ThresholdScreen>(states, routes);
            _flow.StateChanged += OnEntered;
            _flow.Start(ThresholdScreen.Title);
            From = To = Current;
        }

        private static void Add(List<FlowTransition<ThresholdScreen, ThresholdScreen>> routes, ThresholdScreen from, params ThresholdScreen[] destinations)
        {
            foreach (var to in destinations) routes.Add(new FlowTransition<ThresholdScreen, ThresholdScreen>(from, to, to));
        }

        public bool Move(ThresholdScreen destination, float seconds)
        {
            if (IsMoving || ModalOpen || !_routes.Exists(route => route.From == Current && route.To == destination)) return false;
            From = Current;
            To = destination;
            _duration = Math.Max(.1f, seconds);
            _elapsed = 0;
            IsMoving = true;
            InteractionChanged?.Invoke(false);
            return true;
        }

        public void Tick(float seconds)
        {
            if (!IsMoving) return;
            _elapsed += Math.Max(0, seconds);
            if (_elapsed < _duration) return;
            IsMoving = false;
            _flow.TrySend(To);
            InteractionChanged?.Invoke(CanPlay);
        }

        public bool SetModal(bool open)
        {
            if (IsMoving || ModalOpen == open) return false;
            ModalOpen = open;
            InteractionChanged?.Invoke(CanPlay);
            return true;
        }

        private void OnEntered(ThresholdScreen screen) => Entered?.Invoke(screen);
        public void Dispose() { _flow.StateChanged -= OnEntered; _flow.Dispose(); Entered = null; InteractionChanged = null; }
    }
}
