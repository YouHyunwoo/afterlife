using ECS.Dev;
using UnityEditor;
using UnityEngine;

namespace Afterlife.Dev.Game
{
    public class EntityInspector : MonoBehaviour
    {
        [SerializeField] private BoxCollider2D boxCollider;

        private Entity _entity;

        private void OnDrawGizmos()
        {
            if (_entity == null)
                if (!TryGetComponent(out _entity)) return;

            var eid = _entity.Id;
            var cm = _entity.ComponentManager as ComponentManager;

            if (cm.HasCitizenTag[eid])
            {
                if (cm.HasHarvestTarget[eid])
                {
                    var resourceId = cm.HarvestTargets[eid].ResourceEntityId;
                    if (cm.HasEntity[resourceId])
                    {
                        var resourcePosition = cm.Entities[resourceId].transform.position;
                        Gizmos.DrawLine(transform.position, resourcePosition);
                        Handles.Label(resourcePosition + Vector3.down, $"{cm.Resources[resourceId].HarvestRate}");
                    }
                }
                Handles.Label(transform.position + Vector3.down, $"State: {GetCitizenState(eid, cm)}");
            }

            if (cm.HasEnemyTag[eid])
            {
                Handles.Label(transform.position + Vector3.down, $"{GetEnemyState(eid, cm)}");
            }

            if (cm.HasHealth[eid])
            {
                Handles.Label(transform.position + Vector3.down * 2, $"Health: {cm.Healths[eid].Current} / {cm.Healths[eid].Max}");
            }

            if (boxCollider != null)
            {
                Gizmos.color = Color.yellow;
                var size = (Vector3)boxCollider.size;
                var center = transform.position;
                var leftBottom = center - size * 0.5f;
                for (int x = 0; x < size.x; x++)
                {
                    for (int y = 0; y < size.y; y++)
                    {
                        var cellCenter = leftBottom + new Vector3(x + 0.5f, y + 0.5f);
                        Gizmos.DrawWireCube(cellCenter, Vector3.one);
                    }
                }
            }
        }

        private string GetCitizenState(int eid, ComponentManager cm)
        {
            if (cm.HasCitizenBuildTag[eid]) return "Build";
            if (cm.HasCitizenChaseTag[eid]) return "Chase";
            if (cm.HasCitizenFightTag[eid]) return "Fight";
            if (cm.HasCitizenHarvestTag[eid]) return "Harvest";
            if (cm.HasCitizenIdleTag[eid]) return "Idle";
            if (cm.HasCitizenMoveTag[eid]) return "Move";
            if (cm.HasCitizenReturnTag[eid]) return "Return";
            return "해당 없음";
        }

        private string GetEnemyState(int eid, ComponentManager cm)
        {
            if (cm.HasEnemyMoveTag[eid]) return "Move";
            if (cm.HasEnemyFightTag[eid]) return "Fight";
            return "해당 없음";
        }
    }
}