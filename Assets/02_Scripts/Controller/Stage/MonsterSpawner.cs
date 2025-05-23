using System;
using UnityEngine;

namespace Afterlife.Controller
{
    public class MonsterSpawner : MonoBehaviour // TODO: Handler로 변경하기
    {
        [SerializeField] ObjectGenerator objectGenerator;
        [SerializeField] View.Monster monsterPrefab;

        [Header("Viewer")]
        public float[] SpawnIntervalPerDay;
        public float ElapsedTime;
        public int Days;

        Model.Stage stage;

        public event Action<View.Monster> OnMonsterSpawned;

        public void SetUp()
        {
            stage = Controller.Instance.Game.Stage;
            SpawnIntervalPerDay = stage.Data.SpawnIntervalPerDay;
            ElapsedTime = 0f;
            Days = 0;

            enabled = true;
        }

        public void TearDown()
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
            if (stage.Map.Field.Has(location)) { return; }

            var monsterObject = objectGenerator.Generate(monsterPrefab.gameObject, location);
            var monster = monsterObject.GetComponent<View.Monster>();

            OnMonsterSpawned?.Invoke(monster);
        }
    }
}