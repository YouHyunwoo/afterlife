using System;
using System.Collections.Generic;
using Afterlife.Dev.State;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class EnemyVisible : CharacterVisible
    {
        protected float detectionRange;
        protected float attackPower;
        protected float attackRange;
        protected float attackInterval;

        [SerializeField]
        protected int aetheron;

        private StateMachine _stateMachine;

        public float DetectionRange => detectionRange;
        public float AttackRange => attackRange;
        public float AttackPower => attackPower;
        public float AttackInterval => attackInterval;
        public int Aetheron => aetheron;

        public event Func<List<Vector2Int>> GetAllInfluencedPositions;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

#if UNITY_EDITOR
            var stateName = _stateMachine?.CurrentState?.GetType().Name ?? "None";
            UnityEditor.Handles.Label(transform.position, $"State: {stateName}");
#endif
        }

        private void Start()
        {
            _stateMachine = new StateMachine();
            _stateMachine.Initialize(
                new EnemyState[]
                {
                    new EnemyMoveState("move"),
                    new EnemyFightState("fight"),
                },
                new EnemyStateContext()
                {
                    EnemyVisible = this,
                    GetAllInfluencedPositions = GetAllInfluencedPositions,
                }
            );
            _stateMachine.Run();
        }

        private void Update()
            => _stateMachine.Update();

        public override void SetData(ObjectData data)
        {
            base.SetData(data);

            if (data is EnemyData enemyData)
            {
                detectionRange = enemyData.DetectionRange;
                attackPower = enemyData.AttackPower;
                attackRange = enemyData.AttackRange;
                attackInterval = enemyData.AttackInterval;
            }
        }
    }
}