using System;
using System.Collections.Generic;

namespace ECS.Dev
{
    public class EntityManager
    {
        private int _nextId = 0;
        private readonly Stack<int> _freeIds = new();

        public event Action<int> OnEntityDestroyed;

        public int Create()
            => _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;

        public void Destroy(int entity)
        {
            OnEntityDestroyed?.Invoke(entity);
            _freeIds.Push(entity);
        }
    }
}