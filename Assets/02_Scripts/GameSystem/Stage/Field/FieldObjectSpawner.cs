using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage.Field
{
    public class FieldObjectSpawner : SystemBase
    {
        [SerializeField] Transform fieldTransform;

        public GameObject Spawn(GameObject prefab, Vector2Int location)
        {
            if (prefab == null) { throw new System.Exception("Prefab cannot be null."); }

            var position = new Vector3(location.x, location.y);
            var instance = Instantiate(prefab, position, Quaternion.identity, fieldTransform);

            if (instance.TryGetComponent(out View.Object @object))
            {
                var map = ServiceLocator.Get<GameManager>().Game.Stage.Map;
                // TODO: 오브젝트에서 map 빼기
                @object.Map = map;
                map.Field.Set(location, instance.transform);
                map.Fog.Invalidate();
            }

            return instance;
        }
    }
}