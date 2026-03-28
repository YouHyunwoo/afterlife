using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev
{
    public class DictLayer<TCell> : Layer
    {
        protected readonly Dictionary<Vector2Int, TCell> cells = new();

        public Dictionary<Vector2Int, TCell> Cells => cells;

        public DictLayer(Vector2Int size, Dictionary<Vector2Int, TCell> cells) : base(size)
        {
            InitializeCells(size, cells);
        }

        private void InitializeCells(Vector2Int size, Dictionary<Vector2Int, TCell> cells)
        {
            foreach (var cell in cells)
            {
                if (0 <= cell.Key.x && cell.Key.x < size.x &&
                    0 <= cell.Key.y && cell.Key.y < size.y)
                    continue;

                this.cells.Add(cell.Key, cell.Value);
            }
        }
    }
}