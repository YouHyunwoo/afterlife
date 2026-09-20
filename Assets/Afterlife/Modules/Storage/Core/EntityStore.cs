using System;
using System.Collections.Generic;
using Moonstone.D3.Domain;

namespace Afterlife.Modules.Storage
{
    // 실행 중인 판의 엔티티 저장소. D3의 비동기 영속 저장소와 구분한다.
    public class EntityStore<T> where T : Entity
    {
        private readonly Dictionary<string, T> _entities = new();
        public void Save(T entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _entities[entity.Id] = entity;
        }
        public void Delete(string id) => _entities.Remove(id);
        public T FindById(string id) => _entities.TryGetValue(id, out var entity) ? entity : null;
        public bool TryFindById(string id, out T entity) => _entities.TryGetValue(id, out entity);
        public IReadOnlyCollection<T> FindAll() => _entities.Values;
    }
}
