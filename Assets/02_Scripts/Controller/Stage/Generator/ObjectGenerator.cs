using UnityEngine;

namespace Afterlife.Controller
{
    public class ObjectGenerator : MonoBehaviour
    {
        [SerializeField] Transform fieldTransform;

        public Model.Stage Stage;

        public void Initialize(Model.Stage stage)
        {
            Stage = stage;
        }

        public GameObject Generate(GameObject prefab, Vector2Int location)
        {
            if (prefab == null) { throw new System.Exception("Prefab cannot be null."); }

            var position = new Vector3(location.x, location.y);
            var instance = Instantiate(prefab, position, Quaternion.identity, fieldTransform);

            if (instance.TryGetComponent(out View.Object @object))
            {
                @object.Map = Stage.Map;
                Stage.Map.Field.Set(location, instance.transform);
                Stage.Map.Fog.Invalidate();
            }

            return instance;
        }
    }
}