namespace ECS.Dev
{
    public interface IComponentManager
    {
        void HandleEntityDestroyed(int entity);
    }
}