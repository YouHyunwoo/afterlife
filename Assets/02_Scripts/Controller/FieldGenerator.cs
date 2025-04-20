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
            var field = new Model.Field
            {
                Data = fieldData,
                VillageCount = fieldData.VillageCount,
                VillagePrefab = fieldData.VillagePrefab,
                ObjectTransforms = null,
                Grid = new Transform[mapSize.x, mapSize.y]
            };

            var villageObjects = GenerateVillages(fieldData.VillageCount, fieldData.VillagePrefab, mapSize, field);

            field.ObjectTransforms = villageObjects;

            return field;
        }

        List<Transform> GenerateVillages(int count, GameObject prefab, Vector2Int mapSize, Model.Field field)
        {
            var villageObjects = new List<Transform>();

            if (count <= 0) { throw new System.Exception("Village count must be greater than 0."); }
            if (prefab == null) { throw new System.Exception("Village prefab cannot be null."); }
            if (field.GetEmptyCount() < count) { throw new System.Exception($"Not enough space to generate {count} villages."); }

            for (int i = 0; i < count; i++)
            {
                var location = new Vector2Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
                if (field.Has(location)) { i--; continue; }
                var position = new Vector3(location.x, location.y);
                var villageObject = Instantiate(prefab, position, Quaternion.identity, fieldTransform);
                if (!villageObject.transform.TryGetComponent(out View.Object village))
                {
                    Debug.LogError($"Village prefab {prefab.name} does not have a Village component.");
                    continue;
                }

                village.Health = 10;

                field.Set(location, villageObject.transform);
                villageObjects.Add(villageObject.transform);
            }

            return villageObjects;
        }

        public GameObject GenerateObject(GameObject prefab, Vector2Int location)
        {
            if (prefab == null) { throw new System.Exception("Prefab cannot be null."); }

            var position = new Vector3(location.x, location.y);
            var instance = Instantiate(prefab, position, Quaternion.identity, fieldTransform);

            return instance;
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