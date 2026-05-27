using UnityEngine;

namespace Afterlife.Dev.Game
{
    public struct VolumeComponent
    {
        public Vector2Int Size;
        public Vector3 CenterOffset;

        public void SetSize(Vector2Int newSize)
        {
            Size = newSize;
            CenterOffset = new Vector3(newSize.x * 0.5f, newSize.y * 0.5f, 0);
        }

        public readonly Vector3 GetAlignedPosition(Vector3 pivot) => pivot + CenterOffset;
    }
}