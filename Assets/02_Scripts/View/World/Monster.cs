using System.Collections;
using System.Collections.Generic;
using Afterlife.Core;
using Afterlife.GameSystem.Stage;
using DG.Tweening;
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
        public float AttackCount = 1f;
        public float CriticalRate = 0.0f;
        public float CriticalDamageMultiplier = 1.2f;
        public float MovementSpeed = 1f;
        public LayerMask TargetLayerMask;
        public ItemDropGroup[] ItemDropGroups;

        [Header("Viewer")]
        public Animator Animator;
        public SpriteRenderer SpriteRenderer;
        public float SqrDetectingRange;
        public float SqrAttackRange;
        public string StateName;
        public int Direction;
        public List<Transform> TargetCandidateTransforms;
        public Transform targetTransform;
        public Vector2Int targetLocation;

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

            Animator = GetComponent<Animator>();
            SpriteRenderer = GetComponentInChildren<SpriteRenderer>();

            SqrDetectingRange = DetectingRange * DetectingRange;
            SqrAttackRange = AttackRange * AttackRange;

            Direction = Random.Range(0, 2) == 0 ? -1 : 1;
            SpriteRenderer.flipX = Direction == -1;

            OnHitEvent += OnHit;
        }

        void OnHit(Object attacker, Object target)
        {
            Animator.SetTrigger("Hit");
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
            TakeDamage(player.AttackPower, null);
            base.Interact(player);
        }

        public void StartPatrol()
        {
            Animator.SetBool("Patrol", true);
            targetLocation = new Vector2Int(
                Random.Range(0, Map.Size.x - 1),
                Random.Range(0, Map.Size.y - 1)
            );
        }
        public void StopPatrol() => Animator.SetBool("Patrol", false);
        public bool PatrolStep()
        {
            return Move(targetLocation);
        }

        bool Move(Vector2Int destination)
        {
            var currentLocation = Vector2Int.FloorToInt((Vector2)transform.position);

            Map.PathFinder.FindPath(currentLocation, destination, out var path);

            if (path.Length <= 1) { return false; }

            var nextLocation = path[1];

            var direction = nextLocation.x - transform.position.x;
            if (direction != 0 && (direction < 0) != SpriteRenderer.flipX)
            {
                Direction = direction > 0 ? 1 : -1;
                SpriteRenderer.flipX = direction < 0;
            }

            Map.MoveFieldObject(currentLocation, nextLocation);

            transform.position = new Vector3(
                nextLocation.x,
                nextLocation.y,
                transform.position.z
            );

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
                if (sqrDistance < SqrDetectingRange && sqrDistance < minSqrDistance)
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

        public void StartChase() => Animator.SetBool("Chase", true);
        public void StopChase() => Animator.SetBool("Chase", false);
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
            return Vector2.SqrMagnitude(targetTransform.position - transform.position) < SqrDetectingRange;
        }
        public bool IsInDetectingRange(float sqrDistance)
        {
            if (targetTransform == null) { return false; }
            return sqrDistance < SqrDetectingRange;
        }

        public bool IsInAttackRange()
        {
            if (targetTransform == null) { return false; }
            return Vector2.SqrMagnitude(targetTransform.position - transform.position) < SqrAttackRange + 0.001f;
        }
        public bool IsInAttackRange(float sqrDistance)
        {
            if (targetTransform == null) { return false; }
            return sqrDistance < SqrAttackRange + 0.001f;
        }

        public void StartAttack() => Animator.SetBool("Attack", true);
        public void StopAttack() => Animator.SetBool("Attack", false);
        public virtual void AttackStep()
        {
            if (targetTransform == null) { return; }
            if (!targetTransform.TryGetComponent<Object>(out var target)) { return; }

            var damage = AttackPower * AttackCount;
            if (Random.Range(0f, 1f) < CriticalRate)
            {
                damage *= CriticalDamageMultiplier;
            }

            target.TakeDamage(damage, this);
        }

        public override void Died()
        {
            IsAlive = false;
            var location = Vector2Int.FloorToInt(transform.position);
            Map.Field.Set(location, null);
            Animator.SetBool("Dead", true);

            StartCoroutine(CollectByKillRoutine());

            SpriteRenderer.DOFade(0f, 1f).OnComplete(() =>
            {
                gameObject.SetActive(false);
                Destroy(gameObject, 0);
            });
        }

        IEnumerator CollectByKillRoutine()
        {
            var itemCollectSystem = ServiceLocator.Get<ItemCollectSystem>();

            foreach (var itemDropGroup in ItemDropGroups)
            {
                yield return new WaitForSeconds(0.3f);

                var itemId = itemDropGroup.Id;
                var itemAmount = Mathf.FloorToInt(itemDropGroup.Amount * MaxHealth / 10f);
                var itemDropRate = itemDropGroup.DropRate;
                var itemActualAmount = itemCollectSystem.SampleItems(itemAmount, itemDropRate);
                if (itemActualAmount <= 0) { continue; }
                itemCollectSystem.CollectWithRate(itemId, itemActualAmount);
                itemCollectSystem.ShowPopup(transform.position + new Vector3(.5f, .5f), itemId, itemActualAmount);
            }
        }
    }
}