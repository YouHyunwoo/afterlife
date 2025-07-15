using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

namespace Afterlife.Core
{
    public enum ApplicationState
    {
        None,
        Initializing,
        Running,
        Paused,
        PendingExit,
        Exiting,
    }

    public class ApplicationManager : ManagerBase
    {
        [Header("Managers")]
        [SerializeField] ManagerBase[] managers;

        [Header("Starter")]
        [SerializeField] UnityEvent onStartEvent;

        [Header("Application State")]
        public ApplicationState CurrentState;
        bool isQuitReserved;

        void Awake()
        {
            CurrentState = ApplicationState.Initializing;

            ServiceLocator.Register(this);

            foreach (var manager in managers)
            {
                if (manager == null) { continue; }

                ServiceLocator.Register(manager);
            }
        }

        void Start()
        {
            enabled = false;

            SetUp();
        }

        void LateUpdate()
        {
            if (!isQuitReserved) { return; }

            enabled = false;
            isQuitReserved = false;

            QuitInternal();
        }

        void QuitInternal()
        {
            TearDown();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public override void SetUp()
        {
            Localization.Load();

            foreach (var manager in managers)
            {
                if (manager == null) { continue; }

                manager.SetUp();
            }

            CurrentState = ApplicationState.Running;
            onStartEvent?.Invoke();
        }

        public override void TearDown()
        {
            CurrentState = ApplicationState.Exiting;

            DOTween.KillAll();

            foreach (var manager in managers)
            {
                if (manager == null) { continue; }

                manager.TearDown();
            }
        }

        public void Quit()
        {
            isQuitReserved = true;
            enabled = true;

            CurrentState = ApplicationState.PendingExit;
        }
    }
}