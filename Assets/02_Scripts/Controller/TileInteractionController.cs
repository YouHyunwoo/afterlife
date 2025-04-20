using UnityEngine;

namespace Afterlife.Controller
{
    public class TileInteractionController : MonoBehaviour
    {
        [SerializeField] View.TerrainTileIndicator terrainTileIndicator;

        Model.Player player;
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

            if (tileTransform.TryGetComponent(out View.Object @object))
            {
                @object.Interact(player);
            }
        }

        public void Initialize(Model.Player player, Model.Stage stage)
        {
            this.player = player;
            this.stage = stage;
        }
    }
}