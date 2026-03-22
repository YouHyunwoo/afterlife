using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class ConstructionGuideSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private SpriteRenderer _guideSpriteRendererPrefab;

        private SpriteRenderer _guideSpriteRenderer;
        private Material _material;

        protected override void OnInitialize()
        {
            _guideSpriteRenderer = Instantiate(_guideSpriteRendererPrefab);
            _material = _guideSpriteRenderer.material;
        }

        protected override void OnSetUp()
        {
            _guideSpriteRenderer.gameObject.SetActive(true);
        }

        protected override void OnTearDown()
        {
            _guideSpriteRenderer.gameObject.SetActive(false);
        }

        public void ShowGuide(Vector2Int position, Vector2Int size, bool canBuild)
        {
            _guideSpriteRenderer.transform.position = (Vector3)(Vector2)position + (Vector3)(Vector2)size / 2f;
            _guideSpriteRenderer.transform.localScale = new Vector3(size.x, size.y, 1);
            _material.SetVector("_Tiling", new Vector2(size.x, size.y));
            _material.SetFloat("_IsValid", canBuild ? 1.0f : 0.0f);
        }
    }
}