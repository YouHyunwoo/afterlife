// * ComponentManager.Placeable

using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public VolumeComponent[] Volumes = new VolumeComponent[MAX_ENTITIES];

        public bool[] HasVolume = new bool[MAX_ENTITIES];

        public void AddVolume(int entityId, Vector2Int size)
        {
            Volumes[entityId].SetSize(size);
            HasVolume[entityId] = true;
        }

        private void RemoveVolume(int entityId)
        {
            HasVolume[entityId] = false;
        }
    }
}