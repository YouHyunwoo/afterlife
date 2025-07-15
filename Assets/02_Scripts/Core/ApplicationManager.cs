using DG.Tweening;
using UnityEngine;

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

    public class ApplicationManager : MonoBehaviour
    {
        [Header("Managers")]
        public DataManager dataManager;
        public SceneManager sceneManager;
        public InputManager inputManager;
        public AudioManager audioManager;
        public EffectManager effectManager;
        public UIManager uiManager;
        public GameManager gameManager;
        public StageManager stageManager;

        [Header("Application State")]
        public ApplicationState CurrentState;
        bool isQuitReserved;

        void Awake()
        {
            ServiceLocator.Register(this);
            ServiceLocator.Register(dataManager);
            ServiceLocator.Register(sceneManager);
            ServiceLocator.Register(inputManager);
            ServiceLocator.Register(audioManager);
            ServiceLocator.Register(effectManager);
            ServiceLocator.Register(uiManager);
            ServiceLocator.Register(gameManager);
            ServiceLocator.Register(stageManager);
        }

        void Start()
        {
            enabled = false;

            CurrentState = ApplicationState.Initializing;

            Localization.Load();

            uiManager.TitleController.SetUp();
            uiManager.IntroductionController.SetUp();
            uiManager.MainController.SetUp();
            uiManager.StageController.SetUp();
            uiManager.GameOverController.SetUp();
            uiManager.DemoController.SetUp();
            uiManager.HideAll();

            CurrentState = ApplicationState.Running;

            sceneManager.ChangeState(SceneState.Title);
        }

        void LateUpdate()
        {
            if (!isQuitReserved) { return; }

            enabled = false;
            isQuitReserved = false;

            QuitInternal();
        }

        public void Quit()
        {
            isQuitReserved = true;
            enabled = true;

            CurrentState = ApplicationState.PendingExit;
        }

        void QuitInternal()
        {
            CurrentState = ApplicationState.Exiting;

            DOTween.KillAll();

            uiManager.TitleController.TearDown();
            uiManager.IntroductionController.TearDown();
            uiManager.MainController.TearDown();
            uiManager.StageController.TearDown();
            uiManager.GameOverController.TearDown();
            uiManager.DemoController.TearDown();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}