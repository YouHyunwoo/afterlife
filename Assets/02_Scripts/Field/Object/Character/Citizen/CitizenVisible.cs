using Afterlife.Dev.State;
using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenVisible : CharacterVisible
    {
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private GridSystem _gridSystem;
        private CitizenStateMachine _stateMachine;

        private void Start()
        {
            _stateMachine = new CitizenStateMachine();
            _stateMachine.Initialize(
                new CitizenState[]
                {
                    new CitizenIdleState("idle"),
                    new CitizenWanderState("wander"),
                },
                new CitizenStateContext()
                {
                    CitizenVisible = this,
                    TownAreaSystem = _townAreaSystem,
                    GridSystem = _gridSystem,
                }
            );
            _stateMachine.Run();
        }

        private void Update()
        {
            _stateMachine.Update();
        }

        public void SetTownAreaSystem(TownAreaSystem townAreaSystem)
            => _townAreaSystem = townAreaSystem;
        
        public void SetGridSytem(GridSystem gridSystem)
            => _gridSystem = gridSystem;

        public void DoCommand(CommandType command, object[] args)
        {
            if (command == CommandType.Move)
            {
                if (args == null || args.Length != 1) return;

                var destination = (Vector3)args[0];
                _stateMachine.Transit("wander", null, new object[] { destination });
            }
        }
    }
}