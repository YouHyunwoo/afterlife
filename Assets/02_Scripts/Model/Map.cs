using UnityEngine;

namespace Afterlife.Model
{
    public class Map
    {
        public Data.Map Data;
        public Vector2Int Size;
        public Terrain Terrain;
        public Field Field;
        public Fog Fog;
        public Algorithm.PathFinding.AStar.PathFinder PathFinder;

        public bool IsAvailable(Vector2Int location)
        {
            if (!Field.IsInBounds(location)) { return false; }
            if (!Terrain.IsPassable(location.x, location.y)) { return false; }
            if (Field.Has(location.x, location.y)) { return false; }
            return true;
        }

        public void MoveFieldObject(Vector2Int from, Vector2Int to)
        {
            var @object = Field.Get(from);
            Field.Set(from, null);
            Field.Set(to, @object);
            Fog.Invalidate();
        }
    }
}