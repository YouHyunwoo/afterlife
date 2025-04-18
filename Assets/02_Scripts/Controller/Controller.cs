using UnityEngine;

namespace Afterlife.Controller
{
    public class Controller : MonoBehaviour
    {
        [SerializeField] Camera mainCamera;
        [SerializeField] TerrainGenerator terrainGenerator;
        [SerializeField] Data.Map[] mapDataList;

        void Start()
        {
            var terrain = terrainGenerator.Generate(mapDataList[0]);

            mainCamera.transform.position = new Vector3(terrain.Size.x / 2f, terrain.Size.y / 2f, -10f);
        }
    }
}