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
        ToBeExiting,
        Exiting,
    }

    public class ApplicationManager : MonoBehaviour
    {
        public ApplicationState CurrentState;
        bool isQuitReserved;

        [Header("Managers")]
        public DataManager dataManager;
        public SceneManager sceneManager;
        public InputManager inputManager;
        public AudioManager audioManager;
        public UIManager uiManager;
        public GameManager gameManager;
        public StageManager stageManager;

        void Awake()
        {
            ServiceLocator.Register(this);
            ServiceLocator.Register(dataManager);
            ServiceLocator.Register(sceneManager);
            ServiceLocator.Register(inputManager);
            ServiceLocator.Register(audioManager);
            ServiceLocator.Register(uiManager);
            ServiceLocator.Register(gameManager);
            ServiceLocator.Register(stageManager);
        }

        void Start()
        {
            enabled = false;

            Debug.Log("[System] 어플리케이션 시작");

            CurrentState = ApplicationState.Initializing;

            Localization.Load();

            uiManager.TitleController.SetUp();
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

            CurrentState = ApplicationState.ToBeExiting;
        }

        void QuitInternal()
        {
            CurrentState = ApplicationState.Exiting;

            DOTween.KillAll();

            uiManager.TitleController.TearDown();
            uiManager.MainController.TearDown();
            uiManager.StageController.TearDown();
            uiManager.GameOverController.TearDown();
            uiManager.DemoController.TearDown();

            Debug.Log("[System] 어플리케이션 종료");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}