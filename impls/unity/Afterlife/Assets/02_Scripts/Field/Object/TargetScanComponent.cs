using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class TargetScanComponent
    {
        public Func<Vector3, float, IEnumerable<Object>> QueryObjects;

        public Object FindPriorityTarget(Vector3 position, float range, Object self)
        {
            var hits = QueryObjects?.Invoke(position, range);
            if (hits == null) return null;

            Object best1st = null;
            Object best2nd = null;
            float dist1st = float.MaxValue;
            float dist2nd = float.MaxValue;

            foreach (var obj in hits)
            {
                if (obj == self) continue;

                float d = Vector2.Distance(position, obj.Position);

                // 1순위: 시민
                bool is1st = obj is Citizen;
                // 2순위: 건물
                bool is2nd = !is1st && obj is Building;

                if (is1st && d < dist1st) { best1st = obj; dist1st = d; }
                else if (is2nd && d < dist2nd) { best2nd = obj; dist2nd = d; }
            }

            return best1st ?? best2nd;
        }
    }
}
