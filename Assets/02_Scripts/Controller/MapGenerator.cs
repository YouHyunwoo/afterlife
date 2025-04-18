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

            GenerateEnvironments(field, mapData);

            return new Model.Map
            {
                Data = mapData,
                Size = size,
                Terrain = terrain,
                Field = field
            };
        }

        void GenerateEnvironments(Model.Field field, Data.Map mapData)
        {
            var environmentObjects = new List<Transform>();

            for (int i = 0; i < 10; i++)
            {
                var location = new Vector2Int(Random.Range(0, mapData.Size.x), Random.Range(0, mapData.Size.y));
                if (field.Has(location)) { continue; }
                var @object = fieldGenerator.GenerateObject("Tree", location);
                field.Set(location, @object.transform);
                environmentObjects.Add(@object.transform);
            }

            field.ObjectTransforms.AddRange(environmentObjects);
        }
    }
}