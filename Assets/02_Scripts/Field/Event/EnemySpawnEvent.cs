using Moonstone.Ore;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemySpawnEvent : Event
    {
        private EnemyVisible _enemyVisiblePrefab;
        private Container _container;
        private TownAreaSystem _townAreaSystem;

        private float _duration = 10f;
        private float _spawnInterval = 1f;
        private float _elapsedTime;
        private float _spawnTime;

        public EnemySpawnEvent(float triggerTime, EnemyVisible enemyVisiblePrefab) : base(triggerTime)
        {
            _enemyVisiblePrefab = enemyVisiblePrefab;
        }

        protected override void OnStart()
        {
            _spawnTime = Time.time + _spawnInterval;
            _elapsedTime = Time.time + _duration;
        }

        protected override void OnUpdate()
        {
            if (Time.time >= _spawnTime)
            {
                _spawnTime = Time.time + _spawnInterval;
                SpawnEnemy();
            }

            if (Time.time >= _elapsedTime)
            {
                End();
            }
        }

        private void SpawnEnemy()
        {
            var enemyVisible = GameObject.Instantiate(_enemyVisiblePrefab);
            _container.Inject(enemyVisible);
            for (var i = 0; i < 100; i++) // Try Count
            {
                var enemyPosition = new Vector2(Random.Range(0, 20), Random.Range(0, 20)); // Grid Size
                if (_townAreaSystem.IsPositionInAnyInfluence(enemyPosition))
                    continue;
                enemyVisible.NavMeshAgent.Warp(enemyPosition);
                break;
            }
        }
    }
}