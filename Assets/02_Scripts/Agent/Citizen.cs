using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Agent
{
    public class Citizen : MonoBehaviour
    {
        [SerializeField] private Transform _houseTransform;
        private NavMeshAgent _navMeshAgent;
        public bool _isObjectObtained;

        private void Awake()
        {
            TryGetComponent(out _navMeshAgent);
            _navMeshAgent.updateRotation = false;
            _navMeshAgent.updateUpAxis = false;
        }

        private void Update()
        {
            if (_navMeshAgent == null) return;
            if (_houseTransform == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (_isObjectObtained)
                {
                    _navMeshAgent.SetDestination(_houseTransform.position);
                    return;
                }
                var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                var plane = new Plane(Vector3.forward, transform.position);
                if (plane.Raycast(new Ray(mousePosition, Vector3.forward), out var distance))
                {
                    var worldPosition = mousePosition + Vector3.forward * distance;
                    _navMeshAgent.SetDestination(worldPosition);
                }
            }
        }

        public void SetHouse(Transform houseTransform)
        {
            _houseTransform = houseTransform;
        }

        public void PickUpObject()
        {
            _isObjectObtained = true;
            _navMeshAgent.SetDestination(_houseTransform.position);
        }

        public void ObtainObject()
        {
            _isObjectObtained = false;
            _navMeshAgent.SetDestination(transform.position);
        }
    }
}