using UnityEngine;

namespace Afterlife.Core
{
    public class TestManager : MonoBehaviour
    {
        void Update()
        {
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
        }
    }
}