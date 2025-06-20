using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage.Field
{
    public class MonsterSpawnSystem : SystemBase
    {
        [SerializeField] FieldObjectSpawner fieldObjectSpawner;
        [SerializeField] View.Monster monsterPrefab;

        Model.Stage stage;

        public float[] SpawnIntervalPerDay;
        public float ElapsedTime;
        public int Days;

        public event Action<View.Monster> OnMonsterSpawned;

        public override void SetUp()
        {
            stage = ServiceLocator.Get<StageManager>().Stage;

            SpawnIntervalPerDay = stage.Data.SpawnIntervalPerDay;
            ElapsedTime = 0f;
            Days = 0;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            OnMonsterSpawned = null;

            SpawnIntervalPerDay = null;
            ElapsedTime = 0f;
            Days = 0;

            stage = null;
        }

        void Update()
        {
            Days = stage.Days;

            if (Days >= SpawnIntervalPerDay.Length)
            {
                enabled = false;
                return;
            }

            if (stage.IsDayTime) { return; }

            ElapsedTime += Time.deltaTime;

            if (ElapsedTime >= SpawnIntervalPerDay[Days])
            {
                ElapsedTime = 0f;
                SpawnMonster();
            }
        }

        void SpawnMonster()
        {
            var location = new Vector2Int(
                UnityEngine.Random.Range(0, stage.Map.Size.x),
                UnityEngine.Random.Range(0, stage.Map.Size.y)
            );
            if (!stage.Map.IsAvailable(location)) { return; }

            var monsterObject = fieldObjectSpawner.Spawn(monsterPrefab.gameObject, location);
            var monster = monsterObject.GetComponent<View.Monster>();

            OnMonsterSpawned?.Invoke(monster);
        }
    }
}