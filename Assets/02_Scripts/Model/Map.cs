using System.Collections.Generic;
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

        public Vector2Int GetTileLocationByScreenPosition(Vector2 pointerInScreen, Camera camera)
        {
            var pointerInWorld = camera.ScreenToWorldPoint(pointerInScreen);
            return Vector2Int.FloorToInt(pointerInWorld);
        }

        public View.Object[] FindObjectsWithCondition(System.Func<View.Object, bool> condition)
        {
            var objects = new List<View.Object>();
            for (int x = 0; x < Field.Size.x; x++)
            {
                for (int y = 0; y < Field.Size.y; y++)
                {
                    var objectTransform = Field.Get(x, y);
                    if (objectTransform == null) { continue; }
                    if (!objectTransform.TryGetComponent<View.Object>(out var @object)) { continue; }
                    if (!@object.IsAlive) { continue; }
                    if (condition(@object))
                    {
                        objects.Add(@object);
                    }
                }
            }
            return objects.ToArray();
        }
    }
}