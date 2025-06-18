using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public static class TerrainFactory
    {
        public static Model.Terrain Create(Data.Terrain terrainData, Vector2Int mapSize)
        {
            var terrain = new Model.Terrain
            {
                Data = terrainData,
                TerrainGrid = new int[mapSize.x, mapSize.y],
                TransformGrid = new Transform[mapSize.x, mapSize.y]
            };

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var tileIndex = 1;
                    terrain.TerrainGrid[x, y] = tileIndex;
                }
            }

            return terrain;
        }
    }
}