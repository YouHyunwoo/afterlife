using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class StageController : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] StageGenerator stageGenerator;
        [SerializeField] Data.Stage stageData;
        [SerializeField] View.TerrainTileIndicator terrainTileIndicator;
        [SerializeField] TileInteractionController tileInteractionController;
        [SerializeField] GameObject mainView;

        public Transform terrainTransform;
        public Transform fieldTransform;

        Model.Player player;
        List<Transform> playerTransforms = new();

        public void StartStage(Model.Player player)
        {
            this.player = player;

            var stage = stageGenerator.Generate(stageData);

            var objects = GameObject.FindGameObjectsWithTag("Player");
            playerTransforms = new();
            for (int i = 0; i < objects.Length; i++)
            {
                playerTransforms.Add(objects[i].transform);
            }

            var monsters = fieldTransform.GetComponentsInChildren<View.Monster>();
            foreach (var monster in monsters)
            {
                Debug.Log($"Monster spawned at {monster.transform.position}");
                monster.TargetCandidateTransforms = playerTransforms;
                monster.OnDied += OnMonsterDied;
            }

            var mapSize = stageData.MapData.Size;
            mainCamera.transform.position = new Vector3(mapSize.x / 2f, mapSize.y / 2f, -10f);

            tileInteractionController.Initialize(player, stage);
        }

        void OnMonsterDied()
        {
            var monsters = fieldTransform.GetComponentsInChildren<View.Monster>();
            if (monsters.Length == 0)
            {
                Debug.Log("All monsters are dead. Stage cleared!");
                FinishStage();
            }
        }

        void FinishStage()
        {
            player = null;
            playerTransforms.Clear();
            foreach (Transform child in terrainTransform) { Destroy(child.gameObject); }
            foreach (Transform child in fieldTransform) { Destroy(child.gameObject); }
            terrainTileIndicator.gameObject.SetActive(false);
            mainView.SetActive(true);
        }
    }
}