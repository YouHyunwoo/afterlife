using UnityEngine;

namespace Afterlife.Controller
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] TerrainGenerator terrainGenerator;
        [SerializeField] FieldGenerator fieldGenerator;
        [SerializeField] FogGenerator fogGenerator;

        public Model.Map Generate(Data.Map mapData)
        {
            var mapSize = mapData.Size;

            var terrainData = mapData.TerrainData;
            var terrain = terrainGenerator.Generate(terrainData, mapSize);

            var fieldData = mapData.FieldData;
            var field = fieldGenerator.Generate(fieldData, mapSize);

            var fog = fogGenerator.Generate(mapSize);
            fog.OnFogUpdated += field.OnFogUpdated;

            var pathFinder = new Algorithm.PathFinding.AStar.PathFinder(location =>
            {
                if (!field.IsInBounds(location)) { return -1; }
                if (field.Has(location.x, location.y)) { return -1; }
                return 0; // TODO: 지형 장애물 판단 로직 추가
            });

            var map = new Model.Map
            {
                Data = mapData,
                Size = mapSize,
                Terrain = terrain,
                Field = field,
                Fog = fog,
                PathFinder = pathFinder,
            };

            return map;
        }

        public void Clear()
        {
            terrainGenerator.Clear();
            fieldGenerator.Clear();
            fogGenerator.Clear();
        }
    }
}