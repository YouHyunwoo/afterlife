using UnityEngine;

namespace Afterlife.Controller
{
    public class TerrainGenerator : MonoBehaviour
    {
        [SerializeField] Transform terrainTransform;

        public Model.Terrain Generate(Data.Terrain terrainData, Vector2Int mapSize)
        {
            var terrain = new Model.Terrain
            {
                Grid = new int[mapSize.x, mapSize.y],
                TileTransforms = new Transform[mapSize.x, mapSize.y]
            };

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var tileIndex = 1;
                    terrain.Grid[x, y] = tileIndex;

                    var tilePrefab = terrainData.TilePrefabs[tileIndex];
                    var terrainTileTransform = Instantiate(tilePrefab, new Vector3(x, y), Quaternion.identity, terrainTransform).transform;
                    terrainTileTransform.name = $"{tilePrefab.name} ({x}, {y})";
                    terrain.TileTransforms[x, y] = terrainTileTransform;
                }
            }

            return terrain;
        }

        public void Clear()
        {
            foreach (Transform child in terrainTransform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}