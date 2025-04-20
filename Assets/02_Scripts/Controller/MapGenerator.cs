using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] TerrainGenerator terrainGenerator;
        [SerializeField] FieldGenerator fieldGenerator;

        public Model.Map Generate(Data.Map mapData)
        {
            var size = mapData.Size;
            var terrainData = mapData.TerrainData;
            var fieldData = mapData.FieldData;

            var terrain = terrainGenerator.Generate(terrainData, size);
            var field = fieldGenerator.Generate(fieldData, size);

            GenerateEnvironments(fieldData, size, field);

            return new Model.Map
            {
                Data = mapData,
                Size = size,
                Terrain = terrain,
                Field = field
            };
        }

        void GenerateEnvironments(Data.Field fieldData, Vector2Int mapSize, Model.Field field)
        {
            var environmentObjects = new List<Transform>();

            foreach (var resourceObjectGroup in fieldData.ResourceObjectGroups)
            {
                for (int i = 0; i < resourceObjectGroup.Count; i++)
                {
                    var location = new Vector2Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
                    if (field.Has(location)) { continue; }
                    var @object = fieldGenerator.GenerateObject(resourceObjectGroup.Prefab, location);
                    if (!@object.transform.TryGetComponent(out View.Resource resource))
                    {
                        Debug.LogError($"Object {resourceObjectGroup.Prefab.name} does not have a Resource component.");
                        continue;
                    }
                    resource.Health = Random.Range(resourceObjectGroup.MinHealth, resourceObjectGroup.MaxHealth + 1);
                    resource.Type = resourceObjectGroup.Name;
                    resource.Amount = Random.Range(resourceObjectGroup.MinAmount, resourceObjectGroup.MaxAmount + 1);
                    field.Set(location, @object.transform);
                    environmentObjects.Add(@object.transform);
                }
            }

            field.ObjectTransforms.AddRange(environmentObjects);
        }
    }
}