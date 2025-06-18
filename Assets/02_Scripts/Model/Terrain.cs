using UnityEngine;

namespace Afterlife.Model
{
    public class Terrain
    {
        public Data.Terrain Data;
        public int[,] TerrainGrid;
        public Transform[,] TransformGrid;

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
                    TerrainGrid[x, y] = 0;
                }
            }

            TransformGrid = null;
            TerrainGrid = null;
            Data = null;
        }
    }
}