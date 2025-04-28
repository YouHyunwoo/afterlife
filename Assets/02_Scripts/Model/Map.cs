using Algorithm.PathFinding.AStar;
using UnityEngine;

namespace Afterlife.Model
{
    public class Map
    {
        public Data.Map Data;
        public Vector2Int Size;
        public Terrain Terrain;
        public Field Field;
        public PathFinder PathFinder;

        public bool FindPath(Vector2Int start, Vector2Int goal, out Vector2Int[] path)
        {
            return PathFinder.FindPath(start, goal, Field.PathFindingGrid, out path);
        }

        public void MoveFieldObject(Vector2Int from, Vector2Int to)
        {
            var @object = Field.Get(from);
            Field.Set(from, null);
            Field.Set(to, @object);
        }
    }
}