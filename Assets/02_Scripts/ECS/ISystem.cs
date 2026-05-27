namespace ECS.Dev
{
    public interface ISystem<TComponentManager> where TComponentManager : IComponentManager
    {
        void Update(float deltaTime, TComponentManager componentManager);
    }
}