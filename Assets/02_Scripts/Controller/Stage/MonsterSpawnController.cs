using System;
using UnityEngine;
using UnityEngine.Events;

namespace Afterlife.Controller
{
    public class MonsterSpawnController : MonoBehaviour
    {
        [SerializeField] ObjectGenerator objectGenerator;
        [SerializeField] View.Monster monsterPrefab;

        [Header("Event")]
        [SerializeField] UnityEvent<View.Monster> onMonsterSpawnedEvent;

        public event Action<View.Monster> OnMonsterSpawned;

        [Header("Viewer")]
        public Model.Stage Stage;
        public int currentDay;
        public float elapsedTime;
        public float[] SpawnIntervalPerDay;

        void Update()
        {
            elapsedTime += Time.deltaTime;
            if (currentDay >= SpawnIntervalPerDay.Length)
            {
                enabled = false;
                return;
            }
            if (elapsedTime >= SpawnIntervalPerDay[currentDay])
            {
                elapsedTime = 0f;
                SpawnMonster();
            }
        }

        void SpawnMonster()
        {
            var location = new Vector2Int(
                UnityEngine.Random.Range(0, Stage.Map.Size.x),
                UnityEngine.Random.Range(0, Stage.Map.Size.y)
            );
            if (Stage.Map.Field.Has(location)) { return; }

            var monsterObject = objectGenerator.Generate(monsterPrefab.gameObject, location);
            var monster = monsterObject.GetComponent<View.Monster>();
            monster.Map = Stage.Map;
            Stage.Map.Field.Set(location, monsterObject.transform);

            onMonsterSpawnedEvent?.Invoke(monster);
            OnMonsterSpawned?.Invoke(monster);
        }

        public void Initialize(Model.Stage stage)
        {
            Stage = stage;
            SpawnIntervalPerDay = stage.Data.spawnIntervalPerDay;
            currentDay = 0;
            elapsedTime = 0f;
            enabled = true;
        }

        public void OnDayChanged(int day)
        {
            currentDay = day;
        }
    }
}