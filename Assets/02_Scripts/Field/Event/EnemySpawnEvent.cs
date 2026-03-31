using System.Collections.Generic;
using System.Linq;
using Afterlife.Dev.Game;
using Afterlife.Dev.World;
using Moonstone.Ore;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemySpawnEvent : Event
    {
        private Player _player;
        private EnemyVisible _enemyVisiblePrefab;
        private Container _container;
        private WorldRepository _worldRepository;
        private World.World _world;

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
            _world = _worldRepository.FindAll().First();
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
            var passablePositions = _world.WorldMap.GetPassablePositions(Vector2Int.one);
            // var enemyPosition = SamplePassablePosition(passablePositions);
            // var enemyVisible = GameObject.Instantiate(_enemyVisiblePrefab, (Vector2)enemyPosition, Quaternion.identity);
            // _container.Inject(enemyVisible);
            // enemyVisible.GetAllInfluencedPositions += _world.WorldMap.GetAllInfluencedPositions;
            // enemyVisible.OnDied += (attacker, ov, sender) =>
            // {
            //     if (ov is EnemyVisible enemyVisible)
            //         _player.Aetheron += enemyVisible.Aetheron;
            // };
        }

        private Vector2Int SamplePassablePosition(List<Vector2Int> passablePositions)
        {
            var index = Random.Range(0, passablePositions.Count);
            var passablePosition = passablePositions[index];
            passablePositions.RemoveAt(index);
            return passablePosition;
        }
    }
}