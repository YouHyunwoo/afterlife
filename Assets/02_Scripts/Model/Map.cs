using UnityEngine;

namespace Afterlife.Model
{
    public class Map
    {
        public Data.Map Data;
        public Vector2Int Size;
        public Terrain Terrain;
        public Field Field;
        public Algorithm.PathFinding.AStar.PathFinder PathFinder;

        public void MoveFieldObject(Vector2Int from, Vector2Int to)
        {
            var @object = Field.Get(from);
            Field.Set(from, null);
            Field.Set(to, @object);
        }
    }
}