using System;
using UnityEngine;

namespace Afterlife.Dev.Grid
{
    public class Grid
    {
        public static void ForEachCellInRadius(Vector3 center, int radius, Action<Vector3Int> action)
        {
            var minX = Mathf.FloorToInt(center.x - radius);
            var maxX = Mathf.CeilToInt(center.x + radius);
            var minY = Mathf.FloorToInt(center.y - radius);
            var maxY = Mathf.CeilToInt(center.y + radius);
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var cellPosition = new Vector3Int(x, y, 0);
                    if (Vector3.Distance(center, cellPosition + new Vector3(0.5f, 0.5f)) <= radius)
                    {
                        action(cellPosition);
                    }
                }
            }
        }
    }
}