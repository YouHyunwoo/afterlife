using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "MapData", menuName = "Afterlife/Data/Map")]
    public class Map : ScriptableObject
    {
        public Vector2Int Size;
        public Terrain TerrainData;
        public Field FieldData;

        [Header("Terrain Generation")]
        [Range(0, 1)]
        public float GenerationFrequency = 0.1f;
        public int GenerationOctaves = 1;
        public float GenerationRadius;
        public float GenerationScale = 1f;
        [Range(0, 1)]
        public float GenerationHeightThreshold;
    }
}