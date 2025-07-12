using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class TileIndicationSystem : SystemBase
    {
        [SerializeField] View.TerrainTileIndicator tileIndicator;

        bool isIndicatorShownOnce;

        public override void SetUp()
        {
            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnPointerMoveEvent += ShowIndicatorOnce;

            isIndicatorShownOnce = false;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            tileIndicator.gameObject.SetActive(false);

            if (!isIndicatorShownOnce)
            {
                var inputManager = ServiceLocator.Get<InputManager>();
                inputManager.OnPointerMoveEvent -= ShowIndicatorOnce;

                isIndicatorShownOnce = false;
            }
            else
            {
                var player = ServiceLocator.Get<GameManager>().Game.Player;
                player.Light.IsActive = false;
            }
        }

        void ShowIndicatorOnce(Vector2 pointerInScreen, Vector2 pointerInWorld, Vector2Int location)
        {
            tileIndicator.gameObject.SetActive(true);
            isIndicatorShownOnce = true;

            var player = ServiceLocator.Get<GameManager>().Game.Player;
            player.Light.IsActive = true;

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.Fog.Invalidate();

            var inputManager = ServiceLocator.Get<InputManager>();
            inputManager.OnPointerMoveEvent -= ShowIndicatorOnce;
        }

        public void SetLocation(Vector2Int location) => tileIndicator.SetLocation(location);
        public void SetColor(Color color) => tileIndicator.SetColor(color);
    }
}