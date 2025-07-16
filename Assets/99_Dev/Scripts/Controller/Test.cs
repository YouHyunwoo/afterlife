using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Controller
{
    public class Test : MonoBehaviour
    {
        public KeyCode TestActivationKey;
        bool isTestActive;

        void Update()
        {
            if (Input.GetKeyDown(TestActivationKey))
            {
                isTestActive = !isTestActive;
            }

            if (!isTestActive) { return; }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                ServiceLocator.Get<StageManager>().SuccessStage();
                Debug.Log("[System] 스테이지 클리어");
            }
            else if (Input.GetKeyDown(KeyCode.F10))
            {
                ServiceLocator.Get<StageManager>().FailStage();
                Debug.Log("[System] 스테이지 실패");
            }
            else if (Input.GetKeyDown(KeyCode.F11))
            {
                Time.timeScale = Time.timeScale == 1f ? 5f : 1f;
            }
            else if (Input.GetKeyDown(KeyCode.F12))
            {
                ServiceLocator.Get<GameManager>().Game.Player.AttackPower = 9999;
            }
        }
    }
}