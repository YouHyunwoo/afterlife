using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "TerrainData", menuName = "Afterlife/Data/Terrain")]
    public class Terrain : ScriptableObject
    {
        public string EnvironmentType;

        public GameObject[] TilePrefabs;
    }
}