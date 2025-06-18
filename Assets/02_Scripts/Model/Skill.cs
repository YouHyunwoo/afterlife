using System;
using System.Collections;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Model
{
    public abstract class Skill
    {
        public enum SkillState { Idle, Casting, PreDelay, Active, PostDelay, CoolDown }

        public Data.Skill SkillData;
        public SkillState State;

        public event Action OnActivatedEvent;
        public event Action OnDeactivatedEvent;
        public event Action OnCooldownStartedEvent;
        public event Action<float> OnCooldownUpdatedEvent;
        public event Action OnCooldownEndedEvent;
        public event Action OnCanceledEvent;

        Coroutine activateRoutine;
        Coroutine cooldownRoutine;

        public Skill(Data.Skill skillData)
        {
            SkillData = skillData;
        }

        public virtual void SetUp() { }
        public virtual void TearDown() => TearDownInternal();

        void TearDownInternal()
        {
            switch (State)
            {
                case SkillState.Active:
                    if (activateRoutine != null)
                    {
                        CoroutineRunner.StopRoutine(activateRoutine);
                        activateRoutine = null;
                    }
                    OnDeactivated();
                    OnDeactivatedEvent?.Invoke();
                    break;
                case SkillState.CoolDown:
                    if (cooldownRoutine != null)
                    {
                        CoroutineRunner.StopRoutine(cooldownRoutine);
                        cooldownRoutine = null;
                    }
                    OnCooldownEndedEvent?.Invoke();
                    break;
            }
            State = SkillState.Idle;
        }

        public void Use()
        {
            if (State != SkillState.Idle) { return; }

            State = SkillState.Casting;
            OnCasting();
        }

        protected virtual void OnCasting() => Next();
        protected virtual void OnPreDelaying() => Next();
        protected virtual void OnActivated() => Activate(SkillData.ActiveDuration);
        protected virtual void OnDeactivated() { }
        protected virtual void OnPostDelaying() => Next();
        protected virtual void OnCoolingDown() => Cooldown(SkillData.CooldownDuration);
        protected virtual void OnReady() { }
        protected virtual void OnCanceled() { }

        protected void Next()
        {
            switch (State)
            {
                case SkillState.Casting:
                    State = SkillState.PreDelay;
                    OnPreDelaying();
                    break;
                case SkillState.PreDelay:
                    State = SkillState.Active;
                    OnActivated();
                    OnActivatedEvent?.Invoke();
                    break;
                case SkillState.Active:
                    OnDeactivated();
                    OnDeactivatedEvent?.Invoke();
                    State = SkillState.PostDelay;
                    OnPostDelaying();
                    break;
                case SkillState.PostDelay:
                    State = SkillState.CoolDown;
                    OnCoolingDown();
                    break;
                case SkillState.CoolDown:
                    State = SkillState.Idle;
                    OnReady();
                    break;
            }
        }

        public void Cancel()
        {
            OnCanceled();
            TearDownInternal();
            OnCanceledEvent?.Invoke();
        }

        protected void Activate(float duration)
        {
            if (duration <= 0) { Next(); return; }
            activateRoutine = CoroutineRunner.StartRoutine(ActivateRoutine(duration));
        }

        IEnumerator ActivateRoutine(float duration)
        {
            yield return new WaitForSeconds(duration);
            activateRoutine = null;
            Next();
        }

        protected void Cooldown(float duration)
        {
            Debug.Log($"Cooldown: {duration}");
            if (duration <= 0) { Next(); return; }
            cooldownRoutine = CoroutineRunner.StartRoutine(CooldownRoutine(duration));
        }

        IEnumerator CooldownRoutine(float duration)
        {
            OnCooldownStartedEvent?.Invoke();

            var elapsedTime = duration;
            while (elapsedTime > 0)
            {
                elapsedTime -= Time.deltaTime;
                OnCooldownUpdatedEvent?.Invoke(elapsedTime / duration);
                yield return null;
            }
            cooldownRoutine = null;

            OnCooldownEndedEvent?.Invoke();

            Next();
        }

        protected void Delay(float duration, Action callback)
        {
            CoroutineRunner.StartRoutine(DelayRoutine(duration, callback));
        }

        IEnumerator DelayRoutine(float duration, Action callback)
        {
            if (duration > 0) { yield return new WaitForSeconds(duration); }
            callback?.Invoke();
        }
    }
}