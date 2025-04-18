using UnityEngine;

namespace Afterlife.Model {
    public class Terrain {
        public Vector2Int Size;
        public int[,] grid;

        public Transform[,] tileTransforms;
    }
}