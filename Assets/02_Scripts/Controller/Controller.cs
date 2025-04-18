using UnityEngine;

namespace Afterlife.Controller
{
    public class Controller : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] StageGenerator stageGenerator;
        [SerializeField] Data.Stage stageData;

        void Start()
        {
            var stage = stageGenerator.Generate(stageData);

            var mapSize = stageData.MapData.Size;
            mainCamera.transform.position = new Vector3(mapSize.x / 2f, mapSize.y / 2f, -10f);
        }
    }
}