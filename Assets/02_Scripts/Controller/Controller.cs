using UnityEngine;

namespace Afterlife.Controller
{
    public class Controller : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] StageGenerator stageGenerator;
        [SerializeField] Data.Stage stageData;
        [SerializeField] TileInteractionController tileInteractionController;

        Model.Player player;

        void Start()
        {
            player = new Model.Player
            {
                AttackPower = 1f,
                AttackSpeed = 1f,
                AttackRange = 1f,
                AttackCount = 1f,
                CriticalRate = 0.0f,
                CriticalDamageMultiplier = 1.2f
            };

            var stage = stageGenerator.Generate(stageData);

            var mapSize = stageData.MapData.Size;
            mainCamera.transform.position = new Vector3(mapSize.x / 2f, mapSize.y / 2f, -10f);

            tileInteractionController.Initialize(player, stage);
        }
    }
}