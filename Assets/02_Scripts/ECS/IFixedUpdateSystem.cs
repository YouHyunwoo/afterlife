namespace ECS.Dev
{
    public interface IFixedUpdateSystem<TComponentManager> where TComponentManager : IComponentManager
    {
        void FixedUpdate(float fixedDeltaTime, TComponentManager componentManager);
    }
}