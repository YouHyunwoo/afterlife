using UnityEngine;

namespace Afterlife.Controller {
    public class Controller : MonoBehaviour {
        [SerializeField] TerrainGenerator terrainGenerator;
        [SerializeField] Data.Map[] mapDataList;

        void Start()
        {
            var terrain = terrainGenerator.Generate(mapDataList[0]);
        }
    }
}