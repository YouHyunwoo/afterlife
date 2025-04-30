using UnityEngine;

namespace Afterlife.Model
{
    public class Field
    {
        public Data.Field Data;
        public Vector2Int Size;
        public Transform[,] TransformGrid;
        public SpriteRenderer[,] SpriteRendererGrid;
        public int Count;

        public bool IsInBounds(Vector2Int location) => IsInBounds(location.x, location.y);
        public bool IsInBounds(int x, int y) => x >= 0 && x < Size.x && y >= 0 && y < Size.y;

        public bool Has(Vector2Int location) => Has(location.x, location.y);
        public bool Has(int x, int y) => TransformGrid[x, y] != null;

        public void Set(Vector2Int location, Transform @object) => Set(location.x, location.y, @object);
        public void Set(int x, int y, Transform @object)
        {
            if (TransformGrid[x, y] != null) { Count--; }
            TransformGrid[x, y] = @object;
            if (@object != null)
            {
                var spriteRenderer = @object.GetComponentInChildren<SpriteRenderer>();
                if (spriteRenderer != null)
                {
                    SpriteRendererGrid[x, y] = spriteRenderer;
                }
            }
            else
            {
                SpriteRendererGrid[x, y] = null;
            }
            Count++;
        }

        public Transform Get(Vector2Int location) => Get(location.x, location.y);
        public Transform Get(int x, int y) => TransformGrid[x, y];

        public int GetEmptyCount() => Size.x * Size.y - Count;

        public void OnFogUpdated(float[,] fogGrid)
        {
            for (int x = 0; x < Size.x; x++)
            {
                for (int y = 0; y < Size.y; y++)
                {
                    if (SpriteRendererGrid[x, y] == null) { continue; }

                    var fogValue = fogGrid[x, y];
                    SpriteRendererGrid[x, y].color = new Color(1f, 1f, 1f, 1 - fogValue);
                }
            }
        }
    }
}