using System;
using UnityEngine;

namespace Afterlife.Dev.Grid
{
    public class Grid
    {
        public static void ForEachCellInRadius(Vector3 center, float radius, Action<Vector2Int> action)
        {
            var minX = Mathf.FloorToInt(center.x - radius);
            var maxX = Mathf.CeilToInt(center.x + radius);
            var minY = Mathf.FloorToInt(center.y - radius);
            var maxY = Mathf.CeilToInt(center.y + radius);
            var offset = new Vector2(0.5f, 0.5f);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var cellCenter = new Vector2(x, y);
                    if (Vector2.Distance(center, cellCenter + offset) <= radius)
                    {
                        action(Vector2Int.FloorToInt(cellCenter));
                    }
                }
            }
        }
    }
}