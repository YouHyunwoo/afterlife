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
                    if (citizen._isObjectObtained)
                    {
                        citizen._isObjectObtained = false;
                        citizen.ObtainObject();
                        Debug.Log("목재 +1 획득!");
                    }
                    else
                    {
                        citizen.PickUpObject();
                        Debug.Log("목재 +1 주움!");
                    }
                }
            }
        }
    }
}