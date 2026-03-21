using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class House : Building
    {
        [SerializeField] private int _townAreaInfluenceRadius = 3;

        public void Build(TownAreaSystem townAreaSystem)
        {
            townAreaSystem.AddInfluence(transform.position + (Vector3)(Vector2)buildingData.Size * 0.5f, _townAreaInfluenceRadius);
        }
    }
}