using System.Collections.Generic;
using System.Linq;
using Afterlife.Dev.Game;
using Afterlife.Dev.World;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public record WaveEntry(EnemyData Data, EnemyVisible Prefab, int Count);

    public class WaveDefinition
    {
        public WaveEntry[] Entries { get; }
        public float SpawnInterval { get; }
        public int EdgeMargin { get; }

        public WaveDefinition(WaveEntry[] entries, float spawnInterval, int edgeMargin = 4)
        {
            Entries = entries;
            SpawnInterval = spawnInterval;
            EdgeMargin = edgeMargin;
        }
    }

    public class WaveEvent : Event
    {
        private WorldRepository _worldRepository;
        private ObjectSpawnSystem _objectSpawnSystem;
        private ObjectSystem _objectSystem;
        private Player _player;

        private readonly WaveDefinition _definition;
        private World.World _world;

        private readonly List<(EnemyData data, EnemyVisible prefab)> _spawnQueue = new();
        private int _spawnIndex;
        private float _nextSpawnTime;

        public WaveEvent(float triggerTime, WaveDefinition definition) : base(triggerTime)
        {
            _definition = definition;
        }

        protected override void OnStart()
        {
            _world = _worldRepository.FindAll().First();

            foreach (var entry in _definition.Entries)
                for (var i = 0; i < entry.Count; i++)
                    _spawnQueue.Add((entry.Data, entry.Prefab));

            _spawnIndex = 0;
            _nextSpawnTime = Time.time;
        }

        protected override void OnUpdate()
        {
            if (_spawnIndex >= _spawnQueue.Count)
            {
                End();
                return;
            }

            if (Time.time < _nextSpawnTime) return;

            var (data, prefab) = _spawnQueue[_spawnIndex];
            var position = SampleEdgePosition();

            if (_objectSpawnSystem.TrySpawn(
                position, prefab, data,
                id => new Enemy(id),
                out var enemy, out _))
            {
                var capturedEnemy = enemy;
                capturedEnemy.Wander.GetTownZonePositions += _world.WorldMap.GetTownZonePositions;
                capturedEnemy.TargetScan.QueryObjects += _objectSystem.QueryObjects;
                capturedEnemy.OnDied += (attacker, obj, sender) =>
                {
                    _player.Aetheron += capturedEnemy.Aetheron;
                    _objectSpawnSystem.Despawn(obj);
                };
            }

            _spawnIndex++;
            _nextSpawnTime = Time.time + _definition.SpawnInterval;
        }

        private Vector2Int SampleEdgePosition()
        {
            var mapSize = _world.WorldMap.Size;
            var margin = _definition.EdgeMargin;
            var allPassable = _world.WorldMap.GetPassablePositions(Vector2Int.one);

            var edgePositions = new List<Vector2Int>();
            foreach (var pos in allPassable)
            {
                var onEdge = pos.x < margin || pos.x > mapSize.x - margin
                          || pos.y < margin || pos.y > mapSize.y - margin;
                if (onEdge) edgePositions.Add(pos);
            }

            var pool = edgePositions.Count > 0 ? edgePositions : allPassable;
            if (pool.Count == 0)
                return new Vector2Int(mapSize.x / 2, mapSize.y / 2);

            return pool[Random.Range(0, pool.Count)];
        }
    }
}
