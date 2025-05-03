using UnityEngine;

namespace Afterlife.Controller
{
    public class ObjectGenerator : MonoBehaviour
    {
        [SerializeField] Transform fieldTransform;

        public GameObject Generate(GameObject prefab, Vector2Int location)
        {
            if (prefab == null) { throw new System.Exception("Prefab cannot be null."); }

            var position = new Vector3(location.x, location.y);
            var instance = Instantiate(prefab, position, Quaternion.identity, fieldTransform);

            if (instance.TryGetComponent(out View.Object @object))
            {
                var map = Controller.Instance.Game.Stage.Map;

                @object.Map = map;
                map.Field.Set(location, instance.transform);
                map.Fog.Invalidate();
            }

            return instance;
        }
    }
}