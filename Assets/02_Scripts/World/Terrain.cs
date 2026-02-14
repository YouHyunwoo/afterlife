using UnityEngine;

namespace Afterlife.Dev.World
{
    public class Terrain
    {
        public Vector2Int Size;
        public Tile[,] Tiles;
        public bool[,] PassableMap;

        public Terrain(Tile[,] tiles)
        {
            Tiles = tiles;

            var width = tiles.GetLength(0);
            var height = tiles.GetLength(1);
            Size = new Vector2Int(width, height);
            PassableMap = new bool[width, height];
            InitializePassableMap();
        }

        private void InitializePassableMap()
        {
            var width = Tiles.GetLength(0);
            var height = Tiles.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var tile = Tiles[x, y];
                    PassableMap[x, y] = tile.Geography == GeographyType.Land || tile.Geography == GeographyType.Beach;
                }
            }
        }
    }
}