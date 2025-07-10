using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public static class FieldFactory
    {
        public static Model.Field Create(Data.Field fieldData, Vector2Int mapSize)
        {
            var field = new Model.Field
            {
                Data = fieldData,
                Size = mapSize,
                TransformGrid = new Transform[mapSize.x, mapSize.y],
                SpriteRendererGrid = new SpriteRenderer[mapSize.x, mapSize.y],
            };

            return field;
        }
    }
}