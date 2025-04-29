using UnityEngine;

namespace Afterlife.Controller
{
    public class StageGenerator : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] MapGenerator mapGenerator;

        public Model.Stage Generate(Data.Stage stageData)
        {
            var mapData = stageData.MapData;
            var map = mapGenerator.Generate(mapData);

            var stage = new Model.Stage
            {
                Data = stageData,
                Map = map,
            };

            return stage;
        }

        public void Clear()
        {
            mapGenerator.Clear();
        }
    }
}