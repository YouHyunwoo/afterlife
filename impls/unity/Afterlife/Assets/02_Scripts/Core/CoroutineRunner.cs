using UnityEngine;
using System.Collections;

namespace Afterlife.Core
{
    /// <summary>
    /// 어디서든 코루틴을 실행할 수 있는 싱글톤 러너
    /// </summary>
    public class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner _instance;
        public static CoroutineRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("CoroutineRunner");
                    _instance = go.AddComponent<CoroutineRunner>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public static Coroutine StartRoutine(IEnumerator routine)
        {
            return Instance.StartCoroutine(routine);
        }

        public static void StopRoutine(Coroutine coroutine)
        {
            if (Instance != null && coroutine != null)
                Instance.StopCoroutine(coroutine);
        }

        public static void StopAllRoutines()
        {
            if (Instance != null)
                Instance.StopAllCoroutines();
        }
    }
}