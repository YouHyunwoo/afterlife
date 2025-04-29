using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Model
{
    public class Field
    {
        public Data.Field Data;
        public Vector2Int Size;
        public Transform[,] TransformGrid;
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
            Count++;
        }

        public Transform Get(Vector2Int location) => Get(location.x, location.y);
        public Transform Get(int x, int y) => TransformGrid[x, y];

        public int GetEmptyCount() => Size.x * Size.y - Count;
    }
}