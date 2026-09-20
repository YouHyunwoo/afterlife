using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class FieldLayer : DictLayer<int>, IWorldMapLayer
    {
        public FieldLayer(Vector2Int size, Dictionary<Vector2Int, int> cells) : base(size, cells) { }

        public bool IsPassable(Vector2Int position, Vector2Int size)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    try
                    {
                        var p = new Vector2Int(position.x + x, position.y + y);
                        if (cells.ContainsKey(p) && cells[p] > 0)
                            return false;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public void SetPassable(Vector2Int position, Vector2Int size, bool isPassable)
        {
            for (var y = 0; y < size.y; y++)
            {
                for (var x = 0; x < size.x; x++)
                {
                    var p = new Vector2Int(position.x + x, position.y + y);
                    if (!cells.ContainsKey(p))
                        cells.Add(p, 0);
                    cells[p] += isPassable ? -1 : +1;
                    if (cells[p] <= 0)
                        cells.Remove(p);
                }
            }
        }
    }
}