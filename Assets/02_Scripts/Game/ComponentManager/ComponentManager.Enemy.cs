// * ComponentManager.Enemy

using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public bool[] HasEnemyTag = new bool[MAX_ENTITIES];
        public bool[] HasEnemyMoveTag = new bool[MAX_ENTITIES];
        public bool[] HasEnemyFightTag = new bool[MAX_ENTITIES];

        public bool[] HasBossTag = new bool[MAX_ENTITIES];
    }
}