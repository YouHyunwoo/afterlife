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

        void Update()
        {
            ElapsedTime += Time.deltaTime;

            if (stage.IsDayTime && ElapsedTime >= DayDataArray[DayIndex].DayObjectSpawn.SpawnInterval)
            {
                ElapsedTime = 0f;
                SpawnObject();
            }
            else if (!stage.IsDayTime && ElapsedTime >= DayDataArray[DayIndex].NightObjectSpawn.SpawnInterval)
            {
                ElapsedTime = 0f;
                SpawnObject();
            }
        }

        void SpawnObject()
        {
            var location = new Vector2Int(
                UnityEngine.Random.Range(0, stage.Map.Size.x),
                UnityEngine.Random.Range(0, stage.Map.Size.y)
            );
            if (!stage.Map.IsAvailable(location)) { return; }

            var objectSpawn = stage.IsDayTime ? DayDataArray[DayIndex].DayObjectSpawn : DayDataArray[DayIndex].NightObjectSpawn;
            var sampledPrefab = objectSpawn.Sample();
            if (sampledPrefab == null) { return; }

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

            OnObjectSpawnedEvent?.Invoke(@object);
        }

        public void OnDayChanged(int dayIndex)
        {
            DayIndex = dayIndex;
            if (DayIndex >= DayDataArray.Length)
            {
                enabled = false;
            }
        }
    }
}