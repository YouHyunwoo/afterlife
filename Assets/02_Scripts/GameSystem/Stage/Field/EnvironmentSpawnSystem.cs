using System;
using Afterlife.Core;
using Afterlife.Data;
using UnityEngine;

namespace Afterlife.GameSystem.Stage.Field
{
    public class EnvironmentSpawnSystem : SystemBase
    {
        [SerializeField] FieldObjectSpawner fieldObjectSpawner;

        Model.Map map;
        Model.Field field;
        float spawnTimer;
        int currentResourceObjectCount;

        public event Action<View.Resource> OnResourceObjectSpawned;

        public override void SetUp()
        {
            map = ServiceLocator.Get<StageManager>().Stage.Map;
            field = map.Field;

            spawnTimer = 0f;
            currentResourceObjectCount = GetCurrentResourceObjectCount();

            enabled = true;
        }

        int GetCurrentResourceObjectCount()
        {
            int count = 0;

            foreach (Transform transform in field.TransformGrid)
            {
                if (transform == null) { continue; }
                if (transform.TryGetComponent<View.Resource>(out var resource))
                {
                    count++;
                    continue;
                }
            }

            return count;
        }

        void Update()
        {
            spawnTimer += Time.deltaTime;
            if (currentResourceObjectCount >= field.Data.MaxResourceObjectCount) { return; }
            if (spawnTimer < field.Data.ResourceObjectSpawnInterval) { return; }
            spawnTimer = 0f;

            if (UnityEngine.Random.value > field.Data.ResourceObjectSpawnProbability) { return; }

            SpawnRandomResourceObject();
        }

        void SpawnRandomResourceObject()
        {
            if (field.Data.ResourceObjectGroups == null || field.Data.ResourceObjectGroups.Length == 0) { return; }

            var location = new Vector2Int(
                UnityEngine.Random.Range(0, field.Size.x),
                UnityEngine.Random.Range(0, field.Size.y)
            );
            if (!map.IsAvailable(location)) { return; }

            var index = UnityEngine.Random.Range(0, field.Data.ResourceObjectGroups.Length);
            var group = field.Data.ResourceObjectGroups[index];
            if (group.Prefab == null) { return; }

            var resourceObject = fieldObjectSpawner.Spawn(group.Prefab, location);
            var resource = resourceObject.GetComponent<View.Resource>();

            OnResourceObjectSpawned?.Invoke(resource);

            currentResourceObjectCount++;
        }

        public override void TearDown()
        {
            enabled = false;

            spawnTimer = 0f;
            currentResourceObjectCount = 0;

            OnResourceObjectSpawned = null;

            field = null;
        }
    }
}