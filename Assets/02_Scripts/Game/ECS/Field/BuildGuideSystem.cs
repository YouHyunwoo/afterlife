using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class BuildGuideSystem
    {
        private readonly SpriteRenderer _guide;
        private readonly Material _material;

        public BuildGuideSystem(SpriteRenderer prefab)
        {
            _guide = Object.Instantiate(prefab);
            _material = _guide.material;
            _guide.gameObject.SetActive(false);
        }

        public void SetUp() => _guide.gameObject.SetActive(true);

        public void TearDown() => _guide.gameObject.SetActive(false);

        public void ShowGuide(Vector2Int position, Vector2Int size, bool canBuild)
        {
            _guide.transform.position = (Vector3)(Vector2)position + (Vector3)(Vector2)size / 2f;
            _guide.transform.localScale = new Vector3(size.x, size.y, 1);
            _material.SetVector("_Tiling", new Vector2(size.x, size.y));
            _material.SetFloat("_IsValid", canBuild ? 1.0f : 0.0f);
        }
    }
}
