using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "MapData", menuName = "Afterlife/Data/Map")]
    public class Map : ScriptableObject
    {
        public Vector2Int Size;
        public Terrain TerrainData;
        public Field FieldData;
    }
}