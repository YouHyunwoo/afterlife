using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class FogGenerator : MonoBehaviour
    {
        [SerializeField] Transform fogTransform;
        [SerializeField] Transform fogPrefab;

        public Model.Fog Generate(Vector2Int mapSize)
        {
            var fog = new Model.Fog
            {
                Size = mapSize,
                FogGrid = new float[mapSize.x, mapSize.y],
                TransformGrid = new Transform[mapSize.x, mapSize.y],
                SpriteRendererGrid = new SpriteRenderer[mapSize.x, mapSize.y],
                Lights = new List<Model.Light>(),
            };

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    var fogTile = Instantiate(fogPrefab, new Vector3(x, y), Quaternion.identity, fogTransform);
                    fogTile.name = $"FogTile ({x}, {y})";
                    fog.TransformGrid[x, y] = fogTile;
                    fog.SpriteRendererGrid[x, y] = fogTile.GetComponentInChildren<SpriteRenderer>();
                    fog.FogGrid[x, y] = 1f;
                }
            }

            fog.Invalidate();

            return fog;
        }

        public void Clear()
        {
            foreach (Transform child in fogTransform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}