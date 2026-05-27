using UnityEngine;

namespace Afterlife.Dev.World
{
    public class FogLayer : ArrayLayer<float>
    {
        public FogLayer(Vector2Int size) : base(size, CreateInitialCells(size)) { }

        private static float[,] CreateInitialCells(Vector2Int size)
        {
            var grid = new float[size.x, size.y];
            for (var x = 0; x < size.x; x++)
                for (var y = 0; y < size.y; y++)
                    grid[x, y] = 1f;
            return grid;
        }

        public float GetFog(Vector2Int position)
            => cells[position.x, position.y];
        public void SetFog(Vector2Int position, float fog)
            => cells[position.x, position.y] = fog;
    }
}
