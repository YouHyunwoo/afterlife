using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public static class FogFactory
    {
        public static Model.Fog Create(Vector2Int mapSize)
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
                    fog.FogGrid[x, y] = 1f;
                }
            }

            fog.Invalidate();

            return fog;
        }
    }
}