using UnityEngine;

namespace Afterlife.Dev.Grid
{
    public class GridViewer : MonoBehaviour
    {
        [SerializeField] private bool showGrid = true;
        [SerializeField] private Color gridColor = Color.cyan;
        [SerializeField] private Vector2Int gridSize = new(10, 10);
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Vector2 offset;

        private void OnDrawGizmos()
        {
            if (!showGrid) return;

            Gizmos.color = gridColor;
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector3 cellPosition = new Vector3(x, y) * cellSize + (Vector3)offset;
                    Gizmos.DrawWireCube(cellPosition, Vector3.one * cellSize);
                }
            }
        }
    }
}