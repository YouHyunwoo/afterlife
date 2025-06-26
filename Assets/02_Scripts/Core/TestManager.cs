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
                Debug.Log("[System] 다음 스테이지 진행");
            }
        }
    }
}