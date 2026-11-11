using System.Collections.Generic;
using Afterlife.GameSystem.Stage.Field;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Afterlife.GameSystem.Stage
{
    public class StageObjectBuildSystem
    {
        readonly Tilemap terrainTilemap;
        readonly TileBase dirtTile;
        readonly TileBase waterTile;
        readonly Tilemap fogTilemap;
        readonly TileBase fogTile;
        readonly FieldObjectSystem fieldObjectSystem;

        public StageObjectBuildSystem(
            Tilemap terrainTilemap,
            TileBase dirtTile,
            TileBase waterTile,
            Tilemap fogTilemap,
            TileBase fogTile,
            FieldObjectSystem fieldObjectSystem)
        {
            this.terrainTilemap = terrainTilemap;
            this.dirtTile = dirtTile;
            this.waterTile = waterTile;
            this.fogTilemap = fogTilemap;
            this.fogTile = fogTile;
            this.fieldObjectSystem = fieldObjectSystem;
        }

        public void Build(Model.Stage stage, out Vector2Int pivot)
        {
            CreateTerrainObjects(stage);
            CreateFogTiles(stage);
            CreateFieldObjects(stage, out pivot);
        }

        public void Clear(Model.Stage stage)
        {
            if (stage == null) { return; }

            ClearSafely(() => stage.Map.Field?.Dispose());
            ClearSafely(() => stage.Map.Fog?.Dispose());
            ClearSafely(() => stage.Map.Terrain?.Dispose());
            ClearSafely(() => terrainTilemap?.ClearAllTiles());
            ClearSafely(() => fogTilemap?.ClearAllTiles());
        }

        void ClearSafely(System.Action clearAction)
        {
            try
            {
                clearAction?.Invoke();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        void CreateTerrainObjects(Model.Stage stage)
        {
            var mapSize = stage.Map.Size;
            var terrain = stage.Map.Terrain;
            var terrainIndexGrid = terrain.TerrainGrid;

            for (int y = 0; y < mapSize.y; y++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    var tileIndex = terrainIndexGrid[x, y];
                    switch (tileIndex)
                    {
                        case 1:
                            terrainTilemap.SetTile(new Vector3Int(x, y, 0), dirtTile);
                            break;
                        case 3:
                            terrainTilemap.SetTile(new Vector3Int(x, y, 0), waterTile);
                            break;
                    }
                }
            }
        }

        void CreateFogTiles(Model.Stage stage)
        {
            if (fogTilemap == null) { throw new System.Exception("Fog tilemap cannot be null."); }
            if (fogTile == null) { throw new System.Exception("Fog tile cannot be null."); }

            var mapSize = stage.Map.Size;
            var fog = stage.Map.Fog;
            fog.BindTilemap(fogTilemap);

            for (int x = 0; x < mapSize.x; x++)
            {
                for (int y = 0; y < mapSize.y; y++)
                {
                    var position = new Vector3Int(x, y, 0);
                    fogTilemap.SetTile(position, fogTile);
                    fogTilemap.SetTileFlags(position, TileFlags.None);
                    fogTilemap.SetColor(position, new Color(0, 0, 0, 1));
                }
            }
        }

        void CreateFieldObjects(Model.Stage stage, out Vector2Int pivot)
        {
            var fieldData = stage.Data.MapData.FieldData;
            GenerateVillages(stage, fieldData, out pivot);
            GenerateEnvironments(stage, fieldData);
        }

        void GenerateVillages(Model.Stage stage, Data.Field fieldData, out Vector2Int pivot)
        {
            pivot = Vector2Int.zero;
            var padding = 2;
            var villageCount = fieldData.VillageCount;
            var villagePrefab = fieldData.VillagePrefab;

            if (villageCount <= 0) { throw new System.Exception("Village count must be greater than 0."); }
            if (villagePrefab == null) { throw new System.Exception("Village prefab cannot be null."); }
            if (stage.Map.Field.GetEmptyCount() < villageCount) { throw new System.Exception($"Not enough space to generate {villageCount} villages."); }

            var mapSize = stage.Map.Size;

            var tileLocations = new List<Vector2Int>();
            var isValid = false;
            while (!isValid)
            {
                tileLocations.Clear();

                var pivotX = Random.Range(padding, mapSize.x - padding);
                var pivotY = Random.Range(padding, mapSize.y - padding);

                pivot = new Vector2Int(pivotX, pivotY);

                int trials = 0;
                for (int i = 0; i < villageCount && trials < 100; i++, trials++)
                {
                    var offsetX = Random.Range(-padding, padding + 1);
                    var offsetY = Random.Range(-padding, padding + 1);
                    var location = new Vector2Int(pivotX + offsetX, pivotY + offsetY);
                    if (!stage.Map.IsAvailable(location)) { i--; continue; }
                    if (tileLocations.Contains(location)) { i--; continue; }
                    tileLocations.Add(location);
                }

                isValid = trials < 100;
            }

            foreach (var location in tileLocations)
            {
                var fieldObject = fieldObjectSystem.Spawn(villagePrefab, location);
                if (!fieldObject.TryGetComponent(out View.Object village))
                {
                    Debug.LogError($"Village prefab {villagePrefab.name} does not have a Village component.");
                    continue;
                }
            }
        }

        void GenerateEnvironments(Model.Stage stage, Data.Field fieldData)
        {
            var mapSize = stage.Map.Size;

            foreach (var resourceObjectGroup in fieldData.ResourceObjectGroups)
            {
                for (int i = 0; i < resourceObjectGroup.Count; i++)
                {
                    var x = Random.Range(0, mapSize.x);
                    var y = Random.Range(0, mapSize.y);
                    var location = new Vector2Int(x, y);
                    if (!stage.Map.IsAvailable(location)) { continue; }

                    var fieldObject = fieldObjectSystem.Spawn(resourceObjectGroup.Prefab, location);
                    if (!fieldObject.TryGetComponent(out View.Resource resource))
                    {
                        Debug.LogError($"Object {resourceObjectGroup.Prefab.name} does not have a Resource component.");
                        continue;
                    }

                    resource.Value = Random.Range(resourceObjectGroup.MinHealth, resourceObjectGroup.MaxHealth + 1);
                }
            }
        }
    }
}
