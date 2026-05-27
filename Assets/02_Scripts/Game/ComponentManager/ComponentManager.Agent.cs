// * ComponentManager.Agent

using ECS.Dev;
using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Game
{
    public partial class ComponentManager : IComponentManager
    {
        public NavMeshAgent[] NavMeshAgents = new NavMeshAgent[MAX_ENTITIES];
        public MoveCommand[] MoveCommands = new MoveCommand[MAX_ENTITIES];
        public WanderComponent[] Wanders = new WanderComponent[MAX_ENTITIES];
        public AttackComponent[] Attacks = new AttackComponent[MAX_ENTITIES];
        public ChaseTargetComponent[] ChaseTargets = new ChaseTargetComponent[MAX_ENTITIES];
        public HolderComponent[] Holders = new HolderComponent[MAX_ENTITIES];
        public ItemHolder[] ItemHolders = new ItemHolder[MAX_ENTITIES];

        public bool[] HasNavMeshAgent = new bool[MAX_ENTITIES];
        public bool[] HasMoveCommand = new bool[MAX_ENTITIES];
        public bool[] HasWander = new bool[MAX_ENTITIES];
        public bool[] HasAttack = new bool[MAX_ENTITIES];
        public bool[] HasChaseTarget = new bool[MAX_ENTITIES];
        public bool[] HasHolder = new bool[MAX_ENTITIES];

        public void AddNavMeshAgent(int entityId, NavMeshAgent navMeshAgent)
        {
            NavMeshAgents[entityId] = navMeshAgent;
            HasNavMeshAgent[entityId] = true;
        }

        private void RemoveNavMeshAgent(int entityId)
        {
            NavMeshAgents[entityId] = null;
            HasNavMeshAgent[entityId] = false;
        }

        public void AddMoveCommand(int entityId, Vector3 destination, bool hasNewTarget)
        {
            MoveCommands[entityId].Destination = destination;
            MoveCommands[entityId].HasNewTarget = hasNewTarget;
            HasMoveCommand[entityId] = true;
        }

        private void RemoveMoveCommand(int entityId)
        {
            HasMoveCommand[entityId] = false;
        }
        
        public void AddWander(int entityId, float chance, float interval)
        {
            Wanders[entityId].Chance = chance;
            Wanders[entityId].Interval = interval;
            Wanders[entityId].DecisionTime = 0f;
            HasWander[entityId] = true;
        }

        private void RemoveWander(int entityId)
        {
            HasWander[entityId] = false;
        }

        public void AddAttack(int entityId, float power, float range, float interval, float detectionRange)
        {
            Attacks[entityId].AttackPower = power;
            Attacks[entityId].AttackRange = range;
            Attacks[entityId].AttackInterval = interval;
            Attacks[entityId].DetectionRange = detectionRange;
            Attacks[entityId].NextAttackTime = 0f;
            Attacks[entityId].NextScanTime = 0f;
            Attacks[entityId].ReturnDelayTimer = -1f;
            HasAttack[entityId] = true;
        }

        private void RemoveAttack(int entityId)
        {
            HasAttack[entityId] = false;
        }

        public void AddChaseTarget(int entityId, int targetId)
        {
            ChaseTargets[entityId].TargetId = targetId;
            HasChaseTarget[entityId] = true;
        }

        private void RemoveChaseTarget(int entityId)
        {
            HasChaseTarget[entityId] = false;
        }

        public void AddHolder(int entityId, ItemHolder itemHolder)
        {
            Holders[entityId].Woods = 0;
            Holders[entityId].Stones = 0;
            ItemHolders[entityId] = itemHolder;
            HasHolder[entityId] = true;
        }

        private void RemoveHolder(int entityId)
        {
            HasHolder[entityId] = false;
            ItemHolders[entityId] = null;
        }
    }
}
