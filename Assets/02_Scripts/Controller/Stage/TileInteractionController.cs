using System.Collections;
using UnityEngine;

namespace Afterlife.Controller
{
    public class TileInteractionController : MonoBehaviour
    {
        [SerializeField] InputController inputController;
        [SerializeField] View.TerrainTileIndicator terrainTileIndicator;

        Model.Player player;
        Model.Map map;

        Coroutine interactRoutine;
        bool isPointerDown;

        public void SetUp()
        {
            player = Controller.Instance.Game.Player;
            map = Controller.Instance.Game.Stage.Map;
            enabled = true;
        }

        public void TearDown()
        {
            terrainTileIndicator.gameObject.SetActive(false);
            player = null;
            map = null;
            enabled = false;
        }

        void OnEnable()
        {
            inputController.OnPointerDownEvent += OnPointerDown;
            inputController.OnPointerUpEvent += OnPointerUp;
            inputController.OnPointerMoveEvent += OnPointerMove;
        }

        void OnDisable()
        {
            inputController.OnPointerDownEvent -= OnPointerDown;
            inputController.OnPointerUpEvent -= OnPointerUp;
            inputController.OnPointerMoveEvent -= OnPointerMove;
        }

        void OnPointerDown(Vector2Int location)
        {
            isPointerDown = true;
            if (interactRoutine != null) { StopCoroutine(interactRoutine); }
            if (!enabled) { return; }
            interactRoutine = StartCoroutine(InteractRoutine(location));
        }

        IEnumerator InteractRoutine(Vector2Int location)
        {
            InteractByLocation(location);

            var waitTime = new WaitForSeconds(player.AttackSpeed / 1f);
            var playerAttackSpeed = player.AttackSpeed;
            while (isPointerDown)
            {
                if (playerAttackSpeed != player.AttackSpeed)
                {
                    playerAttackSpeed = player.AttackSpeed;
                    waitTime = new WaitForSeconds(player.AttackSpeed / 1f);
                }
                yield return waitTime;
                InteractByLocation(inputController.TileIndex);
            }
        }

        void InteractByLocation(Vector2Int location)
        {
            if (!map.Field.IsInBounds(location)) { return; }

            var tileObjectTransform = map.Field.Get(location);
            if (tileObjectTransform == null) { return; }

            if (tileObjectTransform.TryGetComponent(out View.Object @object))
            {
                @object.Interact(player);
                player.TakeExperience(player.AttackPower * player.AttackCount / 10f);
            }
        }

        void OnPointerUp(Vector2Int location)
        {
            isPointerDown = false;
            if (interactRoutine != null)
            {
                StopCoroutine(interactRoutine);
            }
            interactRoutine = null;
        }

        public void OnPointerMove(Vector2Int location)
        {
            terrainTileIndicator.gameObject.SetActive(true);
            player.Light.IsActive = true;
            player.Light.Location = location;
            map.Fog.Invalidate();
        }
    }
}