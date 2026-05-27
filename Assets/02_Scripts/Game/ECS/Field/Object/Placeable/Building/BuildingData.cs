using UnityEngine;

namespace Afterlife.Dev.Game
{
    [CreateAssetMenu(fileName = "New Building", menuName = "Afterlife/BuildingData")]
    public class BuildingData : PlaceableData
    {
        public float Health;
        public BuildingType BuildingType;
        public float TownZoneRadius = 3f;
        public float BuildSpeed = 1f;
    }
}