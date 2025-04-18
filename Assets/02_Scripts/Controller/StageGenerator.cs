using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class StageGenerator : MonoBehaviour
    {
        [Header("Controller")]
        [SerializeField] TerrainGenerator terrainGenerator;

        [Header("View")]
        [SerializeField] Transform fieldTransform;

        public Model.Stage Generate(Data.Stage stageData)
        {
            var mapData = stageData.MapData;
            var fieldData = mapData.FieldData;

            var terrain = terrainGenerator.Generate(mapData);

            var villageObjects = GenerateVillages(fieldData.VillageCount, fieldData.VillagePrefab, mapData.Size);

            var map = new Model.Map {
                Data = mapData,
                Size = mapData.Size,
                Terrain = terrain,
                Field = new Model.Field
                {
                    Data = fieldData,
                    VillageCount = fieldData.VillageCount,
                    VillagePrefab = fieldData.VillagePrefab,
                    ObjectTransforms = villageObjects,
                }
            };

            var stage = new Model.Stage
            {
                Data = stageData,
                Map = map,
            };

            return stage;
        }

        List<Transform> GenerateVillages(int count, GameObject prefab, Vector2Int mapSize)
        {
            var villageObjects = new List<Transform>();

            for (int i = 0; i < count; i++)
            {
                var location = new Vector2Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
                var position = new Vector3(location.x, location.y);
                var villageObject = Instantiate(prefab, position, Quaternion.identity, fieldTransform);
                villageObjects.Add(villageObject.transform);
            }

            return villageObjects;
        }
    }
}