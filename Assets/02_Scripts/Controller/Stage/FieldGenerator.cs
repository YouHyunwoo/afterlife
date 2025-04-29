using UnityEngine;

namespace Afterlife.Controller
{
    public class FieldGenerator : MonoBehaviour
    {
        [SerializeField] Transform fieldTransform;

        public Model.Field Generate(Data.Field fieldData, Vector2Int mapSize)
        {
            var field = new Model.Field
            {
                Data = fieldData,
                Size = mapSize,
                TransformGrid = new Transform[mapSize.x, mapSize.y],
            };

            return field;
        }

        public void Clear()
        {
            foreach (Transform child in fieldTransform)
            {
                Destroy(child.gameObject);
            }
        }
    }
}