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

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var tileIndex = 1;
                    terrain.TerrainGrid[x, y] = tileIndex;
                    terrain.PassableGrid[x, y] = true;
                }
            }

            for (int i = 0; i < 10; i++)
            {
                var x = Random.Range(0, mapSize.x);
                var y = Random.Range(0, mapSize.y);

                var tileIndex = 3;
                terrain.TerrainGrid[x, y] = tileIndex;
                terrain.PassableGrid[x, y] = terrainData.Passables[tileIndex];
            }

            return terrain;
        }
    }
}