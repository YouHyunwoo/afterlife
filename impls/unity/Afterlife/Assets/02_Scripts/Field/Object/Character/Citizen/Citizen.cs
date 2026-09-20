using System;
using Afterlife.Dev.State;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class Citizen : Object
    {
        protected float attackPower;
        protected float attackRange;
        protected float attackInterval;

        protected StateMachine stateMachine;
        protected Object holdable;
        protected int holdingWoods;
        protected int holdingStones;
        protected HarvestResultInfo harvestResultInfo;

        public float AttackPower => attackPower;
        public float AttackRange => attackRange;
        public float AttackInterval => attackInterval;
        public string StateName => stateMachine?.CurrentState?.GetType().Name ?? "None";
        public bool HasHoldings => holdingWoods > 0 || holdingStones > 0;

        public WanderComponent Wander { get; } = new();
        public BuildingLocatorComponent BuildingLocator { get; } = new();

        public event Action<int, int, Citizen, object> OnHoldingsTaken;
        public event Action<Citizen, object> OnHoldingsDropped;
        public event Action<int, int, Citizen, object> OnHoldingsReturned;

        public Citizen(string id) : base(id) { }

        public override void Initialize(ObjectData data) => Initialize((CitizenData)data);

        public void Initialize(CitizenData data)
        {
            base.Initialize(data);

            attackPower = data.AttackPower;
            attackRange = data.AttackRange;
            attackInterval = data.AttackInterval;

            stateMachine = new StateMachine();
            stateMachine.Initialize(
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
                    Model = this,
                }
            );
            stateMachine.Run();
        }

        public override void Update(float deltaTime)
        {
            stateMachine.Update();
        }

        public void TakeHoldings(int woods, int stones)
        {
            holdingWoods += woods;
            holdingStones += stones;
            OnHoldingsTaken?.Invoke(woods, stones, this, this);
        }

        public void DropHoldings()
        {
            holdingWoods = 0;
            holdingStones = 0;
            OnHoldingsDropped?.Invoke(this, this);
        }

        public void ReturnHoldings()
        {
            var holdingWoodsToBeReturn = holdingWoods;
            var holdingStonesToBeReturn = holdingStones;
            holdingWoods = 0;
            holdingStones = 0;
            OnHoldingsReturned?.Invoke(holdingWoodsToBeReturn, holdingStonesToBeReturn, this, this);
        }

        public void DoCommand(CommandType command, object[] args = null)
        {
            if (command == CommandType.Move)
            {
                if (args == null || args.Length != 1) return;
                stateMachine.Transit("move", null, new object[] { args[0] });
            }
            else if (command == CommandType.Harvest)
            {
                if (args == null || args.Length != 1) return;
                stateMachine.Transit("harvest", null, new object[] { args[0] });
            }
            else if (command == CommandType.Build)
            {
                if (args == null || args.Length != 1) return;
                stateMachine.Transit("build", null, new object[] { args[0] });
            }
            else if (command == CommandType.Fight)
            {
                if (args == null || args.Length != 1) return;
                stateMachine.Transit("chase", null, new object[] { args[0] });
            }
            else if (command == CommandType.Return)
            {
                stateMachine.Transit("return");
            }
        }
    }
}
