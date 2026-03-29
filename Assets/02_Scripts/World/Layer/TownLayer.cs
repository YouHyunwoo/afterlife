using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class TownLayer : DictLayer<int>, IWorldMapLayer
    {
        public TownLayer(Vector2Int size, Dictionary<Vector2Int, int> cells) : base(size, cells)
        {
        }

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

        public bool IsInTown(Vector2Int position)
            => cells.TryGetValue(position, out var count) && count > 0;

        public void AddInfluence(Vector3 centerPosition, float radius)
        {
            Grid.Grid.ForEachCellInRadius(centerPosition, radius, cellPosition =>
            {
                cells.TryAdd(cellPosition, 0);
                cells[cellPosition]++;
            });
        }

        public void RemoveInfluence(Vector3 centerPosition, float radius)
        {
            Grid.Grid.ForEachCellInRadius(centerPosition, radius, cellPosition =>
            {
                if (cells.TryGetValue(cellPosition, out _))
                {
                    cells[cellPosition]--;
                    if (cells[cellPosition] <= 0)
                        cells.Remove(cellPosition);
                }
            });
        }

        public List<Vector2Int> GetAllInfluencedPositions()
            => cells.Keys.ToList();
    }
}