using Afterlife.Data;
using UnityEngine;

namespace Afterlife.Controller
{
    public class TerrainGenerator : MonoBehaviour
    {
        public Model.Terrain Generate(Map data)
        {
            var terrain = new Model.Terrain
            {
                Size = data.Size,
                grid = new int[data.Size.x, data.Size.y],
                tileTransforms = new Transform[data.Size.x, data.Size.y]
            };

            for (int x = 0; x < data.Size.x; x++)
            {
                for (int y = 0; y < data.Size.y; y++)
                {
                    var tileIndex = 1;
                    var tilePrefab = data.TilePrefabs[tileIndex];
                    terrain.grid[x, y] = tileIndex;
                    terrain.tileTransforms[x, y] = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity).transform;
                }
            }

            return terrain;
        }
    }
}