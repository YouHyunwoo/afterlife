using Afterlife.Data;
using UnityEngine;

namespace Afterlife.Controller
{
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] Transform terrainTransform;

        public Model.Terrain Generate(Map data)
        {
            var terrain = new Model.Terrain
            {
                Size = data.Size,
                grid = new int[data.Size.x, data.Size.y],
                tileTransforms = new Transform[data.Size.x, data.Size.y]
            };

            for (int y = 0; y < data.Size.y; y++)
            {
                for (int x = 0; x < data.Size.x; x++)
                {
                    var tileIndex = 1;
                    terrain.grid[x, y] = tileIndex;

                    var tilePrefab = data.TilePrefabs[tileIndex];
                    var terrainTileTransform = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity, terrainTransform).transform;
                    terrainTileTransform.name = $"{tilePrefab.name} ({x}, {y})";
                    terrain.tileTransforms[x, y] = terrainTileTransform;
                }
            }

            return terrain;
        }
    }
}