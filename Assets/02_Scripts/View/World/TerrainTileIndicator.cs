using UnityEngine;

namespace Afterlife.View
{
    public class TerrainTileIndicator : MonoBehaviour
    {
        public Vector2Int TileIndex;

        public void OnPointerMove(Vector2Int location)
        {
            transform.position = new Vector3(location.x, location.y, 0f);
        }
    }
}