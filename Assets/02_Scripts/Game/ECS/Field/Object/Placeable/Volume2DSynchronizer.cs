using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class Volume2DSynchronizer : Moonstone.Ore.Local.Entity
    {
        [SerializeField] private BoxCollider2D _collider;

        public void Sync()
        {
            TryGetComponent(out Entity entity);
            var cm = entity.ComponentManager as ComponentManager;
            var size = cm.Volumes[entity.Id].Size;
            _collider.size = size;
        }
    }
}