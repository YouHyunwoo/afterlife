using System;
using System.Collections.Generic;
using Afterlife.Dev.State;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class Enemy : Object
    {
        protected float detectionRange;
        protected float attackPower;
        protected float attackRange;
        protected float attackInterval;
        protected int aetheron;

        protected StateMachine stateMachine;

        public float DetectionRange => detectionRange;
        public float AttackPower => attackPower;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public int Aetheron => aetheron;
        public string StateName => stateMachine?.CurrentState?.GetType().Name ?? "None";

        public WanderComponent Wander { get; } = new();
        public TargetScanComponent TargetScan { get; } = new();

        public Enemy(string id) : base(id) { }

        public override void Initialize(ObjectData data) => Initialize((EnemyData)data);

        public void Initialize(EnemyData data)
        {
            base.Initialize(data);

            detectionRange = data.DetectionRange;
            attackPower = data.AttackPower;
            attackRange = data.AttackRange;
            attackInterval = data.AttackInterval;
            aetheron = data.Aetheron;

            stateMachine = new StateMachine();
            stateMachine.Initialize(
                new EnemyState[]
                {
                    new EnemyMoveState("move"),
                    new EnemyFightState("fight"),
                },
                new EnemyStateContext()
                {
                    Model = this,
                }
            );
            stateMachine.Run();
        }

        public override void Update(float deltaTime)
        {
            stateMachine.Update();
        }
    }
}
