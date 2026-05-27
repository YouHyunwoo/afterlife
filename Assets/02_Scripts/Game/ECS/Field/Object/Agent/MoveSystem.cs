using ECS.Dev;

namespace Afterlife.Dev.Game
{
    // * MoveCommand를 검사해서 이동 처리
    public class MoveSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager componentManager)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (componentManager.HasMoveCommand[i] &&
                    componentManager.HasNavMeshAgent[i])
                {
                    ref var moveCommand = ref componentManager.MoveCommands[i];
                    var navMeshAgent = componentManager.NavMeshAgents[i];
                    if (moveCommand.HasNewTarget)
                    {
                        navMeshAgent.SetDestination(moveCommand.Destination);
                        moveCommand.HasNewTarget = false;
                    }
                }
            }
        }
    }
}