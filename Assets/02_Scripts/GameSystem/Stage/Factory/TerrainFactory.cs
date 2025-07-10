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
                PassableGrid = new bool[mapSize.x, mapSize.y],
                TransformGrid = new Transform[mapSize.x, mapSize.y]
            };

            return terrain;
        }
    }
}