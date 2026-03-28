using System;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class TerrainLayer : ArrayLayer<TerrainCell>, IWorldMapLayer
    {
        private readonly bool[,] _passableMap;

        public TerrainLayer(Vector2Int size, TerrainCell[,] cells) : base(size, cells)
        {
            _passableMap = new bool[size.x, size.y];
            InitializePassableMap();
        }

        private void InitializePassableMap()
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    var cell = cells[x, y];
                    _passableMap[x, y] = (
                        cell.Geography == GeographyType.Land ||
                        cell.Geography == GeographyType.Beach
                    );
                }
            }
        }

        public void SetPassable(Vector2Int position, Vector2Int size, bool isPassable)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    _passableMap[position.x + x, position.y + y] = isPassable;
                }
            }
        }

        public bool IsPassable(Vector2Int position, Vector2Int size)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    try
                    {
                        if (!_passableMap[position.x + x, position.y + y])
                        {
                            return false;
                        }
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}