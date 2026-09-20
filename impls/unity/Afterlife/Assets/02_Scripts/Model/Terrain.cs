using UnityEngine;

namespace Afterlife.Model
{
    public class Terrain
    {
        public Data.Terrain Data;
        public int[,] TerrainGrid;
        public bool[,] PassableGrid;
        public Transform[,] TransformGrid;

        public bool IsPassable(int x, int y)
        {
            if (x < 0 || x >= PassableGrid.GetLength(0) || y < 0 || y >= PassableGrid.GetLength(1))
            {
                return false;
            }
            return PassableGrid[x, y];
        }

        public void Dispose()
        {
            var mapWidth = TerrainGrid.GetLength(0);
            var mapHeight = TerrainGrid.GetLength(1);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    if (TransformGrid[x, y] != null)
                    {
                        Object.Destroy(TransformGrid[x, y].gameObject);
                    }
                    TransformGrid[x, y] = null;
                    PassableGrid[x, y] = true;
                    TerrainGrid[x, y] = 0;
                }
            }

            TransformGrid = null;
            PassableGrid = null;
            TerrainGrid = null;
            Data = null;
        }
    }
}