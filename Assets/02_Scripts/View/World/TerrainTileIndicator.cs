using UnityEngine;

namespace Afterlife.View
{
    public class TerrainTileIndicator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        public Vector2Int TileIndex;

        public void SetLocation(Vector2Int location) => transform.position = new Vector3(location.x, location.y, 0f);
        public void SetColor(Color color) => spriteRenderer.color = color;
    }
}