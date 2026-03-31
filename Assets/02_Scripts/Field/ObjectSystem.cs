using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Field
{
    public class ObjectSystem : Moonstone.Ore.Local.System
    {
        private readonly List<(Object obj, ObjectVisible visible)> _objects = new();
        private readonly List<CharacterEntry> _characters = new();
        private readonly Dictionary<GameObject, Object> _goToModel = new();
        private readonly Dictionary<Object, Object> _frameCollisions = new();
        private readonly List<Object> _pendingUnregister = new();

        private record CharacterEntry(Object Obj, NavMeshAgent Agent, System.Action RefreshCollider);

        public void Register(Object obj, ObjectVisible visible)
        {
            _objects.Add((obj, visible));
            _goToModel[visible.gameObject] = obj;
        }

        public void RegisterCharacter(Object obj, ObjectVisible visible, NavMeshAgent agent, System.Action refreshCollider)
        {
            _characters.Add(new CharacterEntry(obj, agent, refreshCollider));
            _goToModel[visible.gameObject] = obj;
        }

        public void Unregister(Object obj)
            => _pendingUnregister.Add(obj);

        private void FlushUnregister()
        {
            if (_pendingUnregister.Count == 0) return;

            foreach (var obj in _pendingUnregister)
            {
                _objects.RemoveAll(x => x.obj == obj);
                _characters.RemoveAll(x => x.Obj == obj);

                foreach (var (go, model) in _goToModel)
                {
                    if (model != obj) continue;
                    _goToModel.Remove(go);
                    break;
                }
            }
            _pendingUnregister.Clear();
        }

        // Bootstrapper에서 충돌 이벤트를 번역해 호출
        public void RegisterCollision(Object self, Object other)
            => _frameCollisions[self] = other;

        // Enemy AI에서 Physics2D 결과를 모델로 변환
        public Object GetModel(GameObject go)
            => _goToModel.GetValueOrDefault(go);

        public IEnumerable<Object> QueryObjects(Vector3 position, float radius)
        {
            var hits = Physics2D.OverlapCircleAll(position, radius);
            foreach (var hit in hits)
            {
                if (_goToModel.TryGetValue(hit.gameObject, out var obj))
                    yield return obj;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            // 캐릭터: 입력 동기화 → 상태머신 → 출력 반영
            foreach (var entry in _characters)
            {
                var obj = entry.Obj;
                var agent = entry.Agent;

                // 1. 외부 커맨드로 설정된 목적지를 먼저 agent에 반영한 뒤 HasArrived 읽기
                obj.Position = agent.transform.position;
                var destinationBeforeUpdate = obj.Movement.Destination;
                if (destinationBeforeUpdate.HasValue)
                    agent.SetDestination(destinationBeforeUpdate.Value);
                obj.Movement.HasArrived = CheckArrived(agent);
                obj.Collision.Entered = _frameCollisions.GetValueOrDefault(obj);

                // 2. 상태머신 업데이트
                obj.Update(dt);

                // 3. 상태머신이 목적지를 변경했거나 정지 요청한 경우에만 agent에 반영
                if (obj.Movement.ShouldStop)
                {
                    agent.SetDestination(agent.transform.position);
                    obj.Movement.ShouldStop = false;
                    obj.Movement.Destination = null;
                }
                else if (obj.Movement.Destination != destinationBeforeUpdate)
                {
                    if (obj.Movement.Destination.HasValue)
                        agent.SetDestination(obj.Movement.Destination.Value);
                }

                if (obj.Collision.NeedsRefresh)
                {
                    entry.RefreshCollider?.Invoke();
                    obj.Collision.NeedsRefresh = false;
                }
            }

            // 일반 오브젝트
            foreach (var (obj, agent) in _objects)
            {
                obj.Position = agent.transform.position;
                obj.Update(dt);
            }

            _frameCollisions.Clear();
            FlushUnregister();
        }

        private static bool CheckArrived(NavMeshAgent agent)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return false;
            if (agent.pathPending) return false;
            if (agent.remainingDistance > agent.stoppingDistance) return false;
            return !agent.hasPath || agent.velocity.sqrMagnitude < 0.01f;
        }
    }
}
