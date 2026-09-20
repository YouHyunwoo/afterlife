using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Grid
{
    public class Grid
    {
        public static IEnumerable<Vector2Int> GetCellPositionInRadius(Vector3 centerPosition, float radius)
        {
            var minX = Mathf.FloorToInt(centerPosition.x - radius);
            var maxX = Mathf.CeilToInt(centerPosition.x + radius);
            var minY = Mathf.FloorToInt(centerPosition.y - radius);
            var maxY = Mathf.CeilToInt(centerPosition.y + radius);
            var offset = new Vector2(0.5f, 0.5f);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var cellCenter = new Vector2(x, y);
                    if (Vector2.Distance(centerPosition, cellCenter + offset) <= radius)
                        yield return Vector2Int.FloorToInt(cellCenter);
                }
            }
        }
    }
}