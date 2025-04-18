using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class FieldGenerator : MonoBehaviour
    {
        [SerializeField] GameObject[] objectPrefabs;
        [SerializeField] Transform fieldTransform;

        public Model.Field Generate(Data.Field fieldData, Vector2Int mapSize)
        {
            var villageObjects = GenerateVillages(fieldData.VillageCount, fieldData.VillagePrefab, mapSize);

            return new Model.Field
            {
                Data = fieldData,
                VillageCount = fieldData.VillageCount,
                VillagePrefab = fieldData.VillagePrefab,
                ObjectTransforms = villageObjects,
            };
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

        public GameObject GenerateObject(string name, Vector2Int location)
        {
            var prefab = FindPrefabByName(name);

            var position = new Vector3(location.x, location.y);
            var instance = Instantiate(prefab, position, Quaternion.identity, fieldTransform);

            return instance;
        }

        GameObject FindPrefabByName(string name)
        {
            foreach (var prefab in objectPrefabs)
            {
                if (prefab.name == name)
                {
                    return prefab;
                }
            }

            throw new System.Exception($"Prefab with name {name} not found in the objectPrefabs array.");
        }
    }
}