using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Model
{
    public class Field
    {
        public Data.Field Data;
        public int VillageCount;
        public GameObject VillagePrefab;
        public List<Transform> ObjectTransforms;
        public Transform[,] Grid;
        public int Count;

        public bool InBounds(Vector2Int location) => InBounds(location.x, location.y);
        public bool InBounds(int x, int y) => x >= 0 && x < Grid.GetLength(0) && y >= 0 && y < Grid.GetLength(1);

        public bool Has(Vector2Int location) => Has(location.x, location.y);
        public bool Has(int x, int y) => Grid[x, y] != null;

        public void Set(Vector2Int location, Transform @object) => Set(location.x, location.y, @object);
        public void Set(int x, int y, Transform @object)
        {
            if (Grid[x, y] != null) { Count--; }
            Grid[x, y] = @object;
            Count++;
        }

        public Transform Get(Vector2Int location) => Get(location.x, location.y);
        public Transform Get(int x, int y) => Grid[x, y];

        public int GetEmptyCount() => Grid.GetLength(0) * Grid.GetLength(1) - Count;
    }
}