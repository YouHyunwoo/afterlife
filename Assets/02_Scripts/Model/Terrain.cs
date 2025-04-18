using UnityEngine;

namespace Afterlife.Model
{
    public class Terrain {
        public Data.Terrain Data;
        public Vector2Int Size;
        public int[,] Grid;
        public Transform[,] TileTransforms;
    }
}