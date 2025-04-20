using System.Collections;
using UnityEngine;

namespace Afterlife.Controller
{
    public class TileInteractionController : MonoBehaviour
    {
        [SerializeField] InputController inputController;

        Model.Player player;
        Model.Stage stage;

        Coroutine interactRoutine;
        bool isPointerDown;

        void OnEnable()
        {
            inputController.OnPointerDownEvent += OnPointerDown;
            inputController.OnPointerUpEvent += OnPointerUp;
        }

        void OnDisable()
        {
            inputController.OnPointerDownEvent -= OnPointerDown;
            inputController.OnPointerUpEvent -= OnPointerUp;
        }

        void OnPointerDown(Vector2Int location)
        {
            isPointerDown = true;
            if (interactRoutine != null)
            {
                StopCoroutine(interactRoutine);
            }
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

        void OnPointerUp(Vector2Int location)
        {
            isPointerDown = false;
            if (interactRoutine != null)
            {
                StopCoroutine(interactRoutine);
            }
            interactRoutine = null;
        }

        void InteractByLocation(Vector2Int location)
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