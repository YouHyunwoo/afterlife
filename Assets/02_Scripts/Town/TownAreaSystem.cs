using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.Dev.Town
{
    public class TownAreaSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private Tilemap _grid;

        private readonly Dictionary<Vector3Int, int> _areaInfluenceMap = new();

        private void OnDrawGizmos()
        {
            if (_areaInfluenceMap.Count == 0) return;

            Gizmos.color = new Color(1, 0, 0, 0.5f);
            foreach (var cell in _areaInfluenceMap.Keys)
            {
                var worldPos = _grid.CellToWorld(cell) + _grid.cellSize / 2;
                Gizmos.DrawWireCube(worldPos, _grid.cellSize);
            }
        }

        public bool IsInsideTown(Vector3 areaPosition)
        {
            var cellPosition = _grid.WorldToCell(areaPosition);
            return _areaInfluenceMap.ContainsKey(cellPosition) && _areaInfluenceMap[cellPosition] > 0;
        }

        public void AddInfluence(Vector3 centerPosition, int radius)
        {
            Grid.Grid.ForEachCellInRadius(centerPosition, radius, cellPosition =>
            {
                if (_areaInfluenceMap.ContainsKey(cellPosition))
                    _areaInfluenceMap[cellPosition]++;
                else
                    _areaInfluenceMap[cellPosition] = 1;
            });
        }

        public void RemoveInfluence(Vector3 centerPosition, int radius)
        {
            Grid.Grid.ForEachCellInRadius(centerPosition, radius, cellPosition =>
            {
                if (_areaInfluenceMap.ContainsKey(cellPosition))
                {
                    _areaInfluenceMap[cellPosition]--;
                    if (_areaInfluenceMap[cellPosition] <= 0)
                        _areaInfluenceMap.Remove(cellPosition);
                }
            });
        }
    }
}