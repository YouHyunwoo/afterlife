using System;
using System.Collections.Generic;
using Moonstone.Arc.DependencyInjection;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Afterlife.Modules.Threshold.Unity
{
    [DisallowMultipleComponent]
    public sealed class ThresholdHost : MonoBehaviour
    {
        public ThresholdProfile Profile;
        public Camera ViewCamera;
        public Transform PortalEntry;
        public UnityEvent TrialStarted = new UnityEvent();
        public UnityEvent TrialStopped = new UnityEvent();
        public UnityEvent QuitRequested = new UnityEvent();
        public ThresholdSession Session { get; private set; }
        public float ModalAmount { get; private set; }
        public bool ReducedMotion { get; private set; }
        public float AmbienceVolume { get; private set; }
        public event Action<ThresholdScreen> ScreenEntered;
        public event Action<bool> GameplayInteractionChanged;
        private readonly List<ThresholdPart> _parts = new List<ThresholdPart>();
        private ServiceResolver _resolver;
        private bool _trialRunning;

        private void OnEnable()
        {
            if (!Application.isPlaying) return;
            Session = new ThresholdSession();
            ReducedMotion = Profile != null && Profile.ReducedMotion;
            AmbienceVolume = Profile != null ? Profile.AmbienceVolume : .13f;
            _resolver = new ServiceResolver();
            _resolver.Register(Session);
            _resolver.BindEachOther();
            Session.Entered += Entered;
            Session.InteractionChanged += Interaction;
            foreach (var part in GetComponentsInChildren<ThresholdPart>(true)) if (part.isActiveAndEnabled) Register(part);
            ModalAmount = 0;
            Render(0);
        }
        internal void Register(ThresholdPart part) { if (!_parts.Contains(part)) _parts.Add(part); }
        internal void Unregister(ThresholdPart part) => _parts.Remove(part);
        private void Update()
        {
            if (Session == null) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame && !Session.IsMoving && (Session.Current != ThresholdScreen.Lobby || Session.ModalOpen))
                Session.SetModal(!Session.ModalOpen);
            Session.Tick(Time.unscaledDeltaTime);
            Render(Time.unscaledDeltaTime);
        }
        private void Render(float delta)
        {
            float duration = Profile != null ? Mathf.Max(.1f, Profile.ModalFadeSeconds) : .45f;
            ModalAmount = Mathf.MoveTowards(ModalAmount, Session.ModalOpen ? 1 : 0, delta / duration);
            ThresholdPresentationSystem.Pages(_parts, Session, ModalAmount, delta);
            ThresholdPresentationSystem.Camera(_parts, Session, ViewCamera, PortalEntry, ReducedMotion);
        }
        public void Dispatch(ThresholdAction action)
        {
            if (Session == null || Session.IsMoving) return;
            if (action == ThresholdAction.CloseModal) { Session.SetModal(false); return; }
            if (Session.ModalOpen) return;
            if (action == ThresholdAction.OpenModal) { Session.SetModal(true); return; }
            if (action == ThresholdAction.Quit) { QuitRequested.Invoke(); return; }
            ThresholdScreen target;
            switch (action)
            {
                case ThresholdAction.NewGame: target = ThresholdScreen.Lobby; break;
                case ThresholdAction.EnterTrial: target = ThresholdScreen.Playing; break;
                case ThresholdAction.Fail: target = ThresholdScreen.GameOver; break;
                case ThresholdAction.Complete: target = ThresholdScreen.Clear; break;
                case ThresholdAction.Lobby: target = ThresholdScreen.Lobby; break;
                case ThresholdAction.Title: target = ThresholdScreen.Title; break;
                default: return;
            }
            bool travel = ThresholdSession.IsVoidTravel(Session.Current, target);
            bool result = Session.Current == ThresholdScreen.Playing && ThresholdSession.IsResult(target);
            float duration = travel ? Mathf.Clamp(Profile != null ? Profile.CameraTravelSeconds : .95f, .2f, 1)
                : result ? (Profile != null ? Profile.ResultTransitionSeconds : .8f)
                : (Profile != null ? Profile.WorldTransitionSeconds : 2.4f);
            if (Session.Move(target, ReducedMotion ? .65f : duration) && _trialRunning)
            { _trialRunning = false; TrialStopped.Invoke(); }
        }
        public void ReportFailure() => Dispatch(ThresholdAction.Fail);
        public void ReportCompletion() => Dispatch(ThresholdAction.Complete);
        public void SetReducedMotion(bool value) => ReducedMotion = value;
        public void SetAmbienceVolume(float value) => AmbienceVolume = Mathf.Clamp01(value);
        public void ExitApplication() => Application.Quit();
        private void Entered(ThresholdScreen screen)
        {
            if (screen == ThresholdScreen.Playing) { _trialRunning = true; TrialStarted.Invoke(); }
            ScreenEntered?.Invoke(screen);
        }
        private void Interaction(bool enabled) => GameplayInteractionChanged?.Invoke(enabled);
        private void OnDisable()
        {
            if (_trialRunning) { _trialRunning = false; TrialStopped.Invoke(); }
            if (Session != null) { Session.Entered -= Entered; Session.InteractionChanged -= Interaction; Session.Dispose(); Session = null; }
            _resolver?.Dispose(); _resolver = null;
            ModalAmount = 0;
        }
    }
}
