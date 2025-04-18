using UnityEngine;

namespace Afterlife.Controller
{
    public class TileInteractionController : MonoBehaviour
    {
        [SerializeField] View.TerrainTileIndicator terrainTileIndicator;

        Model.Stage stage;

        void OnEnable()
        {
            terrainTileIndicator.OnInteractedEvent += OnInteract;
        }

        void OnDisable()
        {
            terrainTileIndicator.OnInteractedEvent -= OnInteract;
        }

        void OnInteract(Vector2Int location)
        {
            if (!stage.Map.Field.InBounds(location)) { return; }
            var tileTransform = stage.Map.Field.Get(location);
            if (tileTransform == null) { return; }

            Debug.Log($"Tile at {location} is {tileTransform.name}");
        }

        public void Initialize(Model.Stage stage)
        {
            this.stage = stage;
        }
    }
}