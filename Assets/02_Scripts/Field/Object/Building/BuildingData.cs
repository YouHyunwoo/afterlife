using UnityEngine;

namespace Afterlife.Dev.Field
{
    [CreateAssetMenu(fileName = "New Building", menuName = "Afterlife/Building")]
    public class BuildingData : ObjectData
    {
        public BuildingType BuildingType;
        public float TownZoneRadius = 3f;
        public float BuildSpeed = 1f;
    }
}