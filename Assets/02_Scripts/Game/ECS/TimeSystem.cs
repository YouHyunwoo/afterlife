using ECS.Dev;

namespace Afterlife.Dev.Game
{
    public class TimeSystem : ISystem<ComponentManager>
    {
        public void Update(float deltaTime, ComponentManager componentManager)
        {
            componentManager.Game.Time += componentManager.Game.TimeScale * deltaTime;
        }
    }
}