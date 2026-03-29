using System;
using System.Collections.Generic;
using Afterlife.Dev.Game;
using Afterlife.Dev.State;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenVisible : CharacterVisible
    {
        [SerializeField] protected float attackPower;
        [SerializeField] protected float attackRange;
        [SerializeField] protected float attackInterval;

        [SerializeField] private BuildSystem _buildSystem;
        [SerializeField] private Player _player;

        private Transform _holdableVisibleContainerTransform;
        private StateMachine _stateMachine;

        public float AttackPower => attackPower;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;

        public event Func<Vector2Int, Vector2Int, bool> IsPassable;
        public event Func<List<Vector2Int>> GetAllInfluencedPositions;

        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();

#if UNITY_EDITOR
            var stateName = _stateMachine?.CurrentState?.GetType().Name ?? "None";
            UnityEditor.Handles.Label(transform.position, $"State: {stateName}");
#endif
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            _holdableVisibleContainerTransform = transform.Find("Root").Find("Body").Find("Holdings");
        }

        private void Start()
        {
            _stateMachine = new StateMachine();
            _stateMachine.Initialize(
                new CitizenState[]
                {
                    new CitizenIdleState("idle"),
                    new CitizenMoveState("move"),
                    new CitizenHarvestState("harvest"),
                    new CitizenReturnState("return"),
                    new CitizenBuildState("build"),
                    new CitizenChaseState("chase"),
                    new CitizenFightState("fight"),
                },
                new CitizenStateContext()
                {
                    CitizenVisible = this,
                    BuildSystem = _buildSystem,
                    IsPassable = IsPassable,
                    GetAllInfluencedPositions = GetAllInfluencedPositions,
                }
            );
            _stateMachine.Run();
        }

        private void Update()
            => _stateMachine.Update();

        public void DoCommand(CommandType command, object[] args = null)
        {
            if (command == CommandType.Move)
            {
                if (args == null || args.Length != 1) return;

                var destination = args[0];
                _stateMachine.Transit("move", null, new object[] { destination });
            }
            else if (command == CommandType.Harvest)
            {
                if (args == null || args.Length != 1) return;

                var resourceVisible = args[0];
                _stateMachine.Transit("harvest", null, new object[] { resourceVisible });
            }
            else if (command == CommandType.Build)
            {
                if (args == null || args.Length != 1) return;

                var buildingVisible = (BuildingVisible)args[0];
                if (buildingVisible.IsBuilt) return;
                _stateMachine.Transit("build", null, new object[] { buildingVisible });
            }
            else if (command == CommandType.Fight)
            {
                if (args == null || args.Length != 1) return;

                var enemyVisible = args[0];
                _stateMachine.Transit("chase", null, new object[] { enemyVisible });
            }
        }

        public void AddHoldable(HoldableVisible holdableVisible)
        {
            holdableVisible.transform.SetParent(_holdableVisibleContainerTransform, false);
        }

        public void ClearHoldables()
        {
            foreach (Transform child in _holdableVisibleContainerTransform)
                Destroy(child.gameObject);
        }

        public bool GetHoldables(out HoldableVisible[] holdableVisibles)
        {
            holdableVisibles = _holdableVisibleContainerTransform.GetComponentsInChildren<HoldableVisible>();
            return holdableVisibles.Length > 0;
        }

        public void ObtainHoldables()
        {
            if (!GetHoldables(out var holdableVisibles)) return;

            foreach (var holdableVisible in holdableVisibles)
            {
                foreach (var (type, amount) in holdableVisible.Holdings)
                {
                    if (type == "Woods")
                    {
                        _player.Woods += (int)amount;
                    }
                    else if (type == "Stones")
                    {
                        _player.Stones += (int)amount;
                    }
                }
            }

            ClearHoldables();
        }
    }
}