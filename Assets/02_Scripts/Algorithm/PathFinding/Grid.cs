using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Algorithm.PathFinding
{
    public interface IGrid
    {
        bool IsPassable(Vector2Int location);
        bool IsInBounds(Vector2Int location);
    }

    public class Grid : IGrid
    {
        readonly IComparer<Vector2Int> comparer = Comparer<Vector2Int>.Create((a, b) => a.y == b.y ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));

        public Vector2Int Size;
        public SortedSet<Vector2Int> Availables;

        protected int[,] cells;

        public Grid(int width, int height) : this(new Vector2Int(width, height)) { }

        public Grid(Vector2Int size)
        {
            cells = new int[size.x, size.y];
            Size = size;
            Availables = new SortedSet<Vector2Int>(comparer);

            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    Availables.Add(new(x, y));
                }
            }
        }

        public bool IsPassable(Vector2Int location)
        {
            return IsInBounds(location) && cells[location.x, location.y] == 0;
        }

        public bool IsInBounds(Vector2Int location)
        {
            return location.x >= 0 && location.x < Size.x && location.y >= 0 && location.y < Size.y;
        }

        public int Get(Vector2Int location) => cells[location.x, location.y];
        public int Get(int x, int y) => cells[x, y];

        public void Set(Vector2Int location, int value)
        {
            if (cells[location.x, location.y] == value) { return; }
            if (cells[location.x, location.y] == 0 && value != 0) { Availables.Remove(location); }
            else if (cells[location.x, location.y] != 0 && value == 0) { Availables.Add(location); }
            cells[location.x, location.y] = value;
        }

        public bool SampleAvailableLocation(out Vector2Int location)
        {
            var count = Availables.Count;
            if (count <= 0)
            {
                location = Vector2Int.zero;
                return false;
            }

            int randomIndex = Random.Range(0, count);
            location = Availables.ElementAt(randomIndex);
            return true;
        }

        public void Print()
        {
            StringBuilder result = new();

            for (int y = 0; y < Size.y; y++)
            {
                for (int x = 0; x < Size.x; x++)
                {
                    result.Append(cells[x, Size.y - y - 1] + " ");
                }

                result.Append("\n");
            }

            Debug.Log(result);
        }
    }
}