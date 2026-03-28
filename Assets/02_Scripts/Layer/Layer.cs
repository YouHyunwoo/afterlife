using UnityEngine;

namespace Afterlife.Dev
{
    public abstract class Layer
    {
        protected readonly Vector2Int size;

        public Layer(Vector2Int size)
        {
            this.size = size;
        }
    }
}