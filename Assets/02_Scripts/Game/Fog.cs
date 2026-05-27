using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class Fog : Moonstone.Ore.Local.Entity
    {
        [SerializeField] private Sprite _fogSprite;
        [SerializeField] private Transform _fogContainer;

        private SpriteRenderer[,] _renderers;

        public void Render(Vector2Int size)
        {
            ClearFogCellObjects();
            SetUpFogCellObjects(size);
        }

        private void ClearFogCellObjects()
        {
            foreach (Transform child in _fogContainer)
                Destroy(child.gameObject);
            _renderers = null;
        }

        private void SetUpFogCellObjects(Vector2Int size)
        {
            _renderers = new SpriteRenderer[size.x, size.y];

            for (var x = 0; x < size.x; x++)
            {
                for (var y = 0; y < size.y; y++)
                {
                    var go = new GameObject($"FogTile ({x},{y})");
                    go.transform.SetParent(_fogContainer, worldPositionStays: false);
                    go.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = _fogSprite;
                    sr.color = new Color(0f, 0f, 0f, 1f);
                    sr.sortingLayerID = SortingLayer.NameToID("Fog");
                    _renderers[x, y] = sr;
                }
            }
        }

        public void SetRendererAlpha(Vector2Int position, Color color)
            => _renderers[position.x, position.y].color = color;
    }
}