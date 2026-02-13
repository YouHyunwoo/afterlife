using UnityEngine;

namespace Afterlife.View
{
    public class TerrainTileIndicator : MonoBehaviour
    {
        [SerializeField] SpriteRenderer spriteRenderer;
        public Vector2Int TilePosition => Vector2Int.FloorToInt(transform.position);

        public void SetTilePosition(Vector2Int tilePosition) => transform.position = (Vector2)tilePosition;
        public void SetColor(Color color) => spriteRenderer.color = color;
    }
}