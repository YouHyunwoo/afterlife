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
                Grid = new int[data.Size.x, data.Size.y],
                TileTransforms = new Transform[data.Size.x, data.Size.y]
            };

            for (int y = 0; y < data.Size.y; y++)
            {
                for (int x = 0; x < data.Size.x; x++)
                {
                    var tileIndex = 1;
                    terrain.Grid[x, y] = tileIndex;

                    var tilePrefab = data.TerrainData.TilePrefabs[tileIndex];
                    var terrainTileTransform = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity, terrainTransform).transform;
                    terrainTileTransform.name = $"{tilePrefab.name} ({x}, {y})";
                    terrain.TileTransforms[x, y] = terrainTileTransform;
                }
            }

            return terrain;
        }
    }
}