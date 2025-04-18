using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "MapData", menuName = "Afterlife/Data/Map")]
    public class Map : ScriptableObject
    {
        public string EnvironmentType;
        public Vector2Int Size;

        public GameObject[] TilePrefabs;
    }
}