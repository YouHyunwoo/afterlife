using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class ObjectData : ScriptableObject
    {
        public string Name;
        public GameObject Prefab;
        public Vector2Int Volume2DSize;
        public LightComponent Light;
    }
}