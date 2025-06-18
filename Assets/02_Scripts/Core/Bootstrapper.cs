using UnityEngine;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임 시작 시점에 모든 주요 매니저/서비스를 생성 및 등록하는 부트스트랩 스크립트
    /// </summary>
    public class Bootstrapper : MonoBehaviour
    {
        [Header("Managers")]
        public DataManager dataManager;
        public InputManager inputManager;
        public AudioManager audioManager;
        public UIManager uiManager;
        public GameManager gameManager;
        public StageManager stageManager;

        void Awake()
        {
            if (dataManager != null)
                ServiceLocator.Register(dataManager);
            if (inputManager != null)
                ServiceLocator.Register(inputManager);
            if (audioManager != null)
                ServiceLocator.Register(audioManager);
            if (uiManager != null)
                ServiceLocator.Register(uiManager);
            if (gameManager != null)
                ServiceLocator.Register(gameManager);
            if (stageManager != null)
                ServiceLocator.Register(stageManager);
        }
    }
}
