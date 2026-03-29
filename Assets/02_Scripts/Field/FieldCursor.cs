using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class FieldCursor
    {
        public static bool CastToPlane(out Vector3 hitPoint)
        {
            var plane = new Plane(Vector3.forward, Vector3.zero);
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            var isHit = plane.Raycast(ray, out var enter);
            hitPoint = isHit ? ray.GetPoint(enter) : Vector3.zero;
            return isHit;
        }

        public static bool CastToObject(out ObjectVisible objectVisible)
        {
            objectVisible = null;
            var worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            var hits = Physics2D.RaycastAll(worldPoint, Vector2.zero);
            foreach (var hit in hits)
            {
                if (hit.collider.name != "Body") continue;
                var rootObjectVisible = hit.collider.GetComponentInParent<ObjectVisible>();
                if (rootObjectVisible == null) continue;
                objectVisible = rootObjectVisible;
                break;
            }
            return objectVisible != null;
        }
    }
}