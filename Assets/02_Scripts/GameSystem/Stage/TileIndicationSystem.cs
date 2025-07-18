using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class TileIndicationSystem : SystemBase
    {
        [SerializeField] InputSystem inputSystem;
        [SerializeField] View.TerrainTileIndicator tileIndicator;

        Model.Player player;
        Model.Map map;

        public override void SetUp()
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;
            map = ServiceLocator.Get<StageManager>().Stage.Map;

            inputSystem.OnPointerMoveEvent += ShowIndicatorOnce;

            tileIndicator.gameObject.SetActive(false);
            player.Light.IsActive = false;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            player.Light.IsActive = false;
            tileIndicator.gameObject.SetActive(false);

            inputSystem.OnPointerMoveEvent -= ShowIndicatorOnce;

            map = null;
            player = null;
        }

        void ShowIndicatorOnce(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            tileIndicator.gameObject.SetActive(true);
            player.Light.IsActive = true;
            map.Fog.Invalidate();

            inputSystem.OnPointerMoveEvent -= ShowIndicatorOnce;
        }

        public void SetTilePosition(Vector2Int tilePosition) => tileIndicator.SetTilePosition(tilePosition);
        public void SetColor(Color color) => tileIndicator.SetColor(color);
    }
}