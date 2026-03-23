using NavMeshPlus.Components;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class NavigationSystem : Moonstone.Ore.Local.System
    {
        [SerializeField] private NavMeshSurface _navMeshSurface;

        public void BuildNavMesh()
        {
            _navMeshSurface.BuildNavMesh();
        }
    }
}