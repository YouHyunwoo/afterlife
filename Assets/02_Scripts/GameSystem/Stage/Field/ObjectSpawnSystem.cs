using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage.Field
{
    public class ObjectSpawnSystem : SystemBase
    {
        [SerializeField] FieldObjectSystem fieldObjectSpawner;

        Model.Stage stage;

        public Data.Day[] DayDataArray;
        public float ElapsedTimeAlways;
        public float ElapsedTime;
        public int DayIndex;

        public event Action<View.Object> OnObjectSpawnedEvent;

        public override void SetUp()
        {
            stage = ServiceLocator.Get<StageManager>().Stage;

            DayDataArray = stage.Data.DayDataArray;
            ElapsedTime = 0f;
            DayIndex = 0;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            OnObjectSpawnedEvent = null;

            DayDataArray = null;
            ElapsedTime = 0f;
            DayIndex = 0;

            stage = null;
        }

        public override void UpdateSystem()
        {
            if (DayIndex >= DayDataArray.Length) { return; }

            ElapsedTime += Time.deltaTime;
            ElapsedTimeAlways += Time.deltaTime;

            var alwaysData = DayDataArray[DayIndex].AlwaysObjectSpawn;
            if (alwaysData.SpawnInterval > 0 && ElapsedTimeAlways >= alwaysData.SpawnInterval && alwaysData.PrefabWeightGroups.Length > 0)
            {
                ElapsedTimeAlways = 0f;
                SpawnObject(alwaysData);
            }

            if (stage.IsDayTime && ElapsedTime >= DayDataArray[DayIndex].DayObjectSpawn.SpawnInterval)
            {
                ElapsedTime = 0f;
                SpawnObject(DayDataArray[DayIndex].DayObjectSpawn);
            }
            else if (!stage.IsDayTime && ElapsedTime >= DayDataArray[DayIndex].NightObjectSpawn.SpawnInterval)
            {
                ElapsedTime = 0f;
                SpawnObject(DayDataArray[DayIndex].NightObjectSpawn);
            }
        }

        void SpawnObject(Data.ObjectSpawn objectSpawn)
        {
            var location = new Vector2Int(
                UnityEngine.Random.Range(0, stage.Map.Size.x),
                UnityEngine.Random.Range(0, stage.Map.Size.y)
            );
            if (!stage.Map.IsAvailable(location)) { return; }

            var sampledPrefab = objectSpawn.Sample();
            if (sampledPrefab == null) { return; }

            if (sampledPrefab.TryGetComponent<View.Resource>(out var resource))
            {
                var resourceCount = stage.Map.Field.GetObjectCountWithCondition(obj => obj is View.Resource);
                if (resourceCount >= DayDataArray[DayIndex].MaxResourceCount)
                {
                    return;
                }
            }

            var spawnedGameObject = fieldObjectSpawner.Spawn(sampledPrefab, location);
            var @object = spawnedGameObject.GetComponent<View.Object>();

            var value = Mathf.Max(@object.Value * (1 + (UnityEngine.Random.value - 0.5f) * 2 * 0.1f), 0f);
            value *= 1 + stage.ElapsedTime / 60f;
            value *= DayDataArray[DayIndex].ValueMultiplier;
            value = Mathf.CeilToInt(value);
            @object.Value = value;

            if (@object is View.Portal portal)
            {
                portal.OnObjectSpawnedEvent += OnObjectSpawnedEvent;
            }
        }

        public void OnDayChanged(int dayIndex)
        {
            DayIndex = dayIndex;
        }
    }
}