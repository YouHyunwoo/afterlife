using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class RaycastSystem : Moonstone.Ore.Local.System
    {
        public bool Cast(out Vector3 hitPoint)
        {
            var plane = new Plane(Vector3.forward, Vector3.zero);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var isHit = plane.Raycast(ray, out var enter);
            hitPoint = isHit ? ray.GetPoint(enter) : Vector3.zero;
            return isHit;
        }
    }
}