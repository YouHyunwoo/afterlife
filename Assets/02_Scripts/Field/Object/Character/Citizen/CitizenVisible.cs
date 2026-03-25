using Afterlife.Dev.Town;
using UnityEngine;

namespace Afterlife.Dev.Field
{
    public class CitizenVisible : CharacterVisible
    {
        [SerializeField] private TownAreaSystem _townAreaSystem;
        [SerializeField] private GridSystem _gridSystem;
        [SerializeField] private BuildSystem _buildSystem;

        private Transform _holdableVisibleContainerTransform;
        private CitizenStateMachine _stateMachine;

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
            _stateMachine = new CitizenStateMachine();
            _stateMachine.Initialize(
                new CitizenState[]
                {
                    new CitizenIdleState("idle"),
                    new CitizenMoveState("move"),
                    new CitizenHarvestState("harvest"),
                    new CitizenReturnState("return"),
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

        public void SetBuildSystem(BuildSystem buildSystem)
            => _buildSystem = buildSystem;

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
        }

        public bool FindNearestHouseVisible(out HouseVisible nearestHouseVisible)
        {
            nearestHouseVisible = null;

            var objectMap = _buildSystem.ObjectMap;
            if (objectMap.ContainsKey("HouseVisible"))
            {
                var position = transform.position;
                var minHouseVisible = (HouseVisible)null;
                var minDistance = float.MaxValue;
                foreach (var objectVisible in objectMap["HouseVisible"])
                {
                    if (objectVisible is not HouseVisible houseVisible) continue;
                    var targetPosition = houseVisible.transform.position;
                    var distance = Vector3.Distance(position, targetPosition);
                    if (minDistance > distance)
                    {
                        minHouseVisible = houseVisible;
                        minDistance = distance;
                    }
                }
                nearestHouseVisible = minHouseVisible;
            }

            return true;
        }

        public void AddHoldableVisible(HoldableVisible holdableVisible)
        {
            holdableVisible.transform.SetParent(_holdableVisibleContainerTransform, false);
        }

        public void ClearHoldableVisibles()
        {
            foreach (Transform child in _holdableVisibleContainerTransform)
                Destroy(child.gameObject);
        }

        public bool GetHoldableVisibles(out HoldableVisible[] holdableVisibles)
        {
            holdableVisibles = _holdableVisibleContainerTransform.GetComponentsInChildren<HoldableVisible>();
            return holdableVisibles.Length > 0;
        }

        public void ObtainHoldables()
        {
            if (!GetHoldableVisibles(out var holdableVisibles)) return;

            foreach (var holdableVisible in holdableVisibles)
            {
                foreach (var (type, amount) in holdableVisible.Holdings)
                {
                    if (type == "Woods")
                    {
                        Globals.Player.Woods += (int)amount;
                    }
                    else if (type == "Stones")
                    {
                        Globals.Player.Stones += (int)amount;
                    }
                }
            }

            ClearHoldableVisibles();
        }
    }
}