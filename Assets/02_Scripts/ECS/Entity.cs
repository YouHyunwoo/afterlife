using UnityEngine;

namespace ECS.Dev
{
    public class Entity : MonoBehaviour
    {
        public int Id;
        public IComponentManager ComponentManager;
    }
}