namespace ECS.Dev
{
    public class Application<T> where T : IComponentManager
    {
        private ISystem<T>[] _systems;
        private T _componentManager;

        public Application(ISystem<T>[] systems, T componentManager)
        {
            _systems = systems;
            _componentManager = componentManager;
        }

        public void Update(float deltaTime)
        {
            foreach (var system in _systems)
                system.Update(deltaTime, _componentManager);
        }
    }
}