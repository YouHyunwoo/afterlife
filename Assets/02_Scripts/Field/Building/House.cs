using Afterlife.Dev.Agent;
using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class House : Building
    {
        [SerializeField] private int _townAreaInfluenceRadius = 3;

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

        public void Build(TownAreaSystem townAreaSystem)
        {
            townAreaSystem.AddInfluence(transform.position + (Vector3)(Vector2)buildingData.Size * 0.5f, _townAreaInfluenceRadius);
        }

        public void Demolish(TownAreaSystem townAreaSystem)
        {
            townAreaSystem.RemoveInfluence(transform.position + (Vector3)(Vector2)buildingData.Size * 0.5f, _townAreaInfluenceRadius);
        }
    }
}