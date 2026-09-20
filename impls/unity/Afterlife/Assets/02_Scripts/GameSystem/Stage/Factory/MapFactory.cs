namespace Afterlife.GameSystem.Stage
{
    public static class MapFactory
    {
        public static Model.Map Create(Data.Map mapData)
        {
            var mapSize = mapData.Size;

            var terrainData = mapData.TerrainData;
            var terrain = TerrainFactory.Create(terrainData, mapSize);

            var fieldData = mapData.FieldData;
            var field = FieldFactory.Create(fieldData, mapSize);

            var fog = FogFactory.Create(mapSize);
            fog.OnFogUpdated += field.OnFogUpdated;

            var pathFinder = new Algorithm.PathFinding.AStar.PathFinder(location =>
            {
                if (!field.IsInBounds(location)) { return -1; }
                if (!terrain.PassableGrid[location.x, location.y]) { return -1; }
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
    }
}