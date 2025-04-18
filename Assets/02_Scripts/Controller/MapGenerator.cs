using UnityEngine;

namespace Afterlife.Controller {
    public class MapGenerator : MonoBehaviour {
        [SerializeField] TerrainGenerator terrainGenerator;
        [SerializeField] FieldGenerator fieldGenerator;

        public Model.Map Generate(Data.Map mapData) {
            var size = mapData.Size;
            var terrainData = mapData.TerrainData;
            var fieldData = mapData.FieldData;

            var terrain = terrainGenerator.Generate(terrainData, size);
            var field = fieldGenerator.Generate(fieldData, size);

            return new Model.Map {
                Data = mapData,
                Size = size,
                Terrain = terrain,
                Field = field
            };
        }
    }
}