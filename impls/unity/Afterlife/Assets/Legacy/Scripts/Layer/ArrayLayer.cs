using UnityEngine;

namespace Afterlife.Dev
{
    public class ArrayLayer<TCell> : Layer
    {
        protected readonly TCell[,] cells;

        public TCell[,] Cells => cells;

        public ArrayLayer(Vector2Int size, TCell[,] cells) : base(size)
        {
            this.cells = new TCell[size.x, size.y];
            InitializeCells(size, cells);
        }

        private void InitializeCells(Vector2Int size, TCell[,] cells)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    this.cells[x, y] = cells[x, y];
                }
            }
        }
    }
}