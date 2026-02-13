using System.Collections.Generic;
using Afterlife.Core;
using UnityEditor;
using UnityEngine;

namespace Afterlife.View
{
    public class Monster : Object
    {
        [Header("Property")]
        public float DetectingRange = 1f;
        public float AttackPower = 1f;
        public float AttackSpeed = 1f;
        public float AttackRange = 1f;
        public float CriticalRate = 0.0f;
        public float CriticalDamageMultiplier = 1.2f;
        public float MovementSpeed = 1f;
        public LayerMask TargetLayerMask;

        [Header("Viewer")]
        public string StateName;
        public List<Transform> TargetCandidateTransforms;
        public Transform targetTransform;
        public Vector2Int targetLocation;

        float sqrDetectingRange;
        float sqrAttackRange;

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.one * 0.5f, DetectingRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.one * 0.5f, AttackRange);

            Handles.color = Color.white;
            Handles.Label(transform.position + Vector3.down * 0.1f, StateName, EditorStyles.boldLabel);
        }
#endif

        protected override void Awake()
        {
            base.Awake();

            sqrDetectingRange = DetectingRange * DetectingRange;
            sqrAttackRange = AttackRange * AttackRange;
        }

        protected override void Start()
        {
            base.Start();

            // TODO: 타겟 후보 탐색 최적화
            var objects = GameObject.FindGameObjectsWithTag("Player");
            TargetCandidateTransforms = new List<Transform>();
            for (int i = 0; i < objects.Length; i++)
            {
                var targetCandidateTransform = objects[i].transform;
                if (targetCandidateTransform.TryGetComponent<Object>(out var targetCandidateObject))
                {
                    targetCandidateObject.OnDiedEvent += OnTargetDied;
                }
                TargetCandidateTransforms.Add(targetCandidateTransform);
            }
        }

        public override void Interact(Model.Player player)
        {
            ServiceLocator.Get<EffectManager>().PlayGFX("Cut", transform.position);
            ServiceLocator.Get<AudioManager>().PlaySFX("sword");
            var isCriticalHit = Random.value < player.CriticalRate;
            if (isCriticalHit) { ServiceLocator.Get<AudioManager>().PlaySFX("critical"); }
            var damage = player.AttackPower * (isCriticalHit ? player.CriticalDamageMultiplier : 1f);
            player.TakeExperience(damage / 10f);
            TakeDamage(damage, null);
            base.Interact(player);
        }

        public void StartPatrol()
        {
            animator.SetBool("Patrol", true);

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            targetLocation = new Vector2Int(
                Random.Range(0, map.Size.x - 1),
                Random.Range(0, map.Size.y - 1)
            );
        }
        public void StopPatrol() => animator.SetBool("Patrol", false);
        public bool PatrolStep()
        {
            return Move(targetLocation);
        }

        bool Move(Vector2Int destination)
        {
            var currentLocation = Vector2Int.FloorToInt((Vector2)transform.position);

            var map = ServiceLocator.Get<StageManager>().Stage.Map;
            map.PathFinder.FindPath(currentLocation, destination, out var path);

            if (path.Length <= 1) { return false; }

            var nextLocation = path[1];

            var direction = nextLocation.x - transform.position.x;
            if (direction != 0 && (direction < 0) != bodySpriteRenderer.flipX)
            {
                SetDirection(direction > 0 ? ObjectDirection.Right : ObjectDirection.Left);
            }

            map.MoveFieldObject(currentLocation, nextLocation);

            transform.position = new Vector3(
                nextLocation.x,
                nextLocation.y,
                transform.position.z
            );

            RefreshSight();

            return true;
        }

        public bool IsArrived()
        {
            return targetLocation.x == transform.position.x &&
                    targetLocation.y == transform.position.y;
        }

        public bool FindNearestTarget(out Transform targetTransform)
        {
            targetTransform = null;
            var minSqrDistance = float.MaxValue;
            for (int i = 0; i < TargetCandidateTransforms.Count; i++)
            {
                var targetCandidateTransform = TargetCandidateTransforms[i];
                if (targetCandidateTransform == null) { continue; }

                var sqrDistance = Vector2.SqrMagnitude(targetCandidateTransform.position - transform.position);
                if (sqrDistance < sqrDetectingRange && sqrDistance < minSqrDistance)
                {
                    minSqrDistance = sqrDistance;
                    targetTransform = targetCandidateTransform;
                }
            }

            return targetTransform != null;
        }

        public void SetTarget(Transform target)
        {
            targetTransform = target;
        }

        void OnTargetDied(Object attacker, Object target)
        {
            if (!IsAlive) { return; }

            TargetCandidateTransforms.Remove(target.transform);
            if (targetTransform != target.transform) { return; }
            targetTransform = null;
            StopChase();
            StopAttack();
        }

        public void StartChase() => animator.SetBool("Chase", true);
        public void StopChase() => animator.SetBool("Chase", false);
        public bool ChaseStep()
        {
            targetLocation = Vector2Int.FloorToInt((Vector2)targetTransform.position);
            return Move(targetLocation);
        }

        public bool SqrDistanceToTarget(out float sqrDistance)
        {
            sqrDistance = 0f;
            if (targetTransform == null) { return false; }
            sqrDistance = Vector2.SqrMagnitude(targetTransform.position - transform.position);
            return true;

        }
        public bool IsInDetectingRange()
        {
            if (targetTransform == null) { return false; }
            return Vector2.SqrMagnitude(targetTransform.position - transform.position) < sqrDetectingRange;
        }
        public bool IsInDetectingRange(float sqrDistance)
        {
            if (targetTransform == null) { return false; }
            return sqrDistance < sqrDetectingRange;
        }

        public bool IsInAttackRange()
        {
            if (targetTransform == null) { return false; }
            return Vector2.SqrMagnitude(targetTransform.position - transform.position) < sqrAttackRange + 0.001f;
        }
        public bool IsInAttackRange(float sqrDistance)
        {
            if (targetTransform == null) { return false; }
            return sqrDistance < sqrAttackRange + 0.001f;
        }

        public void StartAttack() => animator.SetBool("Attack", true);
        public void StopAttack() => animator.SetBool("Attack", false);
        public virtual void AttackStep()
        {
            if (targetTransform == null) { return; }
            if (!targetTransform.TryGetComponent<Object>(out var target)) { return; }

            var damage = AttackPower;
            if (Random.Range(0f, 1f) < CriticalRate)
            {
                damage *= CriticalDamageMultiplier;
            }

            target.TakeDamage(damage, this);
        }
    }
}