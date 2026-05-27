using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class WaveEvent : Event
    {
        private readonly World _world;
        private readonly WaveDefinition _definition;
        private readonly List<EnemyData> _spawnQueue = new();
        private int _spawnIndex;
        private float _nextSpawnTime;

        public WaveEvent(float triggerTime, WaveDefinition definition, World world) : base(triggerTime)
        {
            _world = world;
            _definition = definition;
        }

        protected override void OnStart()
        {
            foreach (var entry in _definition.Entries)
                for (var i = 0; i < entry.Count; i++)
                    _spawnQueue.Add(entry.Data);

            _spawnIndex = 0;
            _nextSpawnTime = TriggerTime;
        }

        protected override void OnUpdate(float deltaTime, float currentTime)
        {
            if (_spawnIndex >= _spawnQueue.Count) { End(); return; }
            if (currentTime < _nextSpawnTime) return;

            var data = _spawnQueue[_spawnIndex];
            var position = SampleEdgePosition();
            _world.TrySpawn(position, data, out _);

            _spawnIndex++;
            _nextSpawnTime = currentTime + _definition.SpawnInterval;
        }

        private Vector2Int SampleEdgePosition()
        {
            var mapSize = _world.Size;
            var margin = _definition.EdgeMargin;
            var allPassable = _world.GetPassablePositions(Vector2Int.one);

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
