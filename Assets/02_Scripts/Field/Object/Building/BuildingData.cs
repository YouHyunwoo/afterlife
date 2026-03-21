using UnityEngine;

namespace Afterlife.Dev.Field
{
    [CreateAssetMenu(fileName = "New Building", menuName = "Afterlife/Building")]
    public class BuildingData : ObjectData
    {
        public float TownAreaInfluenceRadius = 3f;
    }
}