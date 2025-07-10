using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage.Field
{
    public class FieldObjectSystem : SystemBase
    {
        [SerializeField] Transform fieldTransform;

        Model.Map map;

        public override void SetUp()
        {
            map = ServiceLocator.Get<StageManager>().Stage.Map;
        }

        public override void TearDown()
        {
            map = null;
        }

        public GameObject Spawn(GameObject prefab, Vector2Int location)
        {
            if (prefab == null) { throw new System.Exception("Prefab cannot be null."); }

            var position = new Vector3(location.x, location.y);
            var instance = Instantiate(prefab, position, Quaternion.identity, fieldTransform);

            if (instance.TryGetComponent(out View.Object @object))
            {
                @object.OnDiedEvent += OnDiedEvent;

                map.Field.Set(location, instance.transform);
                map.Fog.Invalidate();
            }

            return instance;
        }

        void OnDiedEvent(View.Object attacker, View.Object @object)
        {
            var location = Vector2Int.FloorToInt(@object.transform.position);
            map.Field.Set(location, null);

            @object.OnDiedEvent -= OnDiedEvent;
        }
    }
}