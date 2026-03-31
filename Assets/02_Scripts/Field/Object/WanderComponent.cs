using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class WanderComponent
    {
        public Func<List<Vector2Int>> GetTownZonePositions;
        public Func<Vector2Int, Vector2Int, bool> IsPassable;

        public bool SampleTownZonePosition(out Vector2 destination)
        {
            destination = Vector2.zero;

            var positions = GetTownZonePositions?.Invoke();
            if (positions == null || positions.Count == 0)
                return false;

            var index = UnityEngine.Random.Range(0, positions.Count);
            destination = (Vector2)positions[index];
            return true;
        }

        public bool FindRandomDestination(out Vector3 destination)
        {
            destination = Vector3.zero;

            var positions = GetTownZonePositions?.Invoke();
            if (positions == null || positions.Count == 0)
                return false;

            while (positions.Count > 0)
            {
                var index = UnityEngine.Random.Range(0, positions.Count);
                var candidate = positions[index];
                positions.RemoveAt(index);

                var position = new Vector2Int(Mathf.RoundToInt(candidate.x), Mathf.RoundToInt(candidate.y));
                var size = Vector2Int.one;

                if (IsPassable != null && IsPassable(position, size))
                {
                    destination = (Vector2)candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
