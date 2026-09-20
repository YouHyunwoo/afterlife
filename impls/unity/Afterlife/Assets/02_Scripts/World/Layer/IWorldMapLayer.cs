using UnityEngine;

namespace Afterlife.Dev.World
{
    public interface IWorldMapLayer
    {
        void SetPassable(Vector2Int position, Vector2Int size, bool isPassable);
        bool IsPassable(Vector2Int position, Vector2Int size);
    }
}