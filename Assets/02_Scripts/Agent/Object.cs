using UnityEngine;

namespace Afterlife.Dev.Agent
{
    public class Object : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (collision.TryGetComponent(out Citizen citizen))
                {
                    citizen.PickUpObject();
                    Debug.Log("목재 +1 주움!");
                }
            }
        }
    }
}