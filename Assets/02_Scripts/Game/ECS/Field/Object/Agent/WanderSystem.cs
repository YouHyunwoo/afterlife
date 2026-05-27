using System;
using System.Collections.Generic;
using ECS.Dev;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    // * 시민이면 Idle 상태일 때 랜덤하게 Wander
    public class WanderSystem : ISystem<ComponentManager>
    {
        public Func<List<Vector2Int>> GetTownZonePositions;
        public Func<Vector2Int, Vector2Int, bool> IsPassable;

        public void Update(float deltaTime, ComponentManager cm)
        {
            for (var i = 0; i < ComponentManager.MAX_ENTITIES; i++)
            {
                if (!cm.HasWander[i]) continue;
                if (!cm.HasMoveCommand[i]) continue;

                if (cm.HasCitizenIdleTag[i])
                    UpdateCitizenWander(i, cm);
            }
        }

        private void UpdateCitizenWander(int entityId, ComponentManager cm)
        {
            ref var wander = ref cm.Wanders[entityId];

            var currentTime = Time.time;
            if (currentTime < wander.DisabledTime)
            {
                wander.DecisionTime = currentTime + wander.Interval;
                return;
            }

            if (currentTime < wander.DecisionTime)
                return;

            wander.DecisionTime = currentTime + wander.Interval;

            if (UnityEngine.Random.value < wander.Chance)
            {
                if (FindRandomDestination(out var destination))
                {
                    cm.MoveCommands[entityId].Destination = destination;
                    cm.MoveCommands[entityId].HasNewTarget = true;
                    cm.HasCitizenIdleTag[entityId] = false;
                    cm.HasCitizenMoveTag[entityId] = true;
                }
            }
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