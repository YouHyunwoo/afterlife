using Afterlife.Dev.Agent;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class HouseVisible : BuildingVisible
    {
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision.CompareTag("Player"))
            {
                if (collision.TryGetComponent(out Citizen citizen))
                {
                    if (citizen._isObjectObtained)
                    {
                        citizen.ObtainObject();
                        Debug.Log("목재 +1 획득!");
                    }
                }
            }
        }
    }
}