using UnityEngine;
using UnityEngine.AI;

namespace Afterlife.Dev.Agent
{
    public class Citizen : MonoBehaviour
    {
        [SerializeField] Transform _homeTransform;
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
            if (_isObjectObtained)
            {
                _navMeshAgent.SetDestination(_homeTransform.position);
            }
            else
            {
                if (Input.GetMouseButtonDown(0))
                {
                    var mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                    var plane = new Plane(Vector3.forward, transform.position);
                    if (plane.Raycast(new Ray(mousePosition, Vector3.forward), out var distance))
                    {
                        var worldPosition = mousePosition + Vector3.forward * distance;
                        _navMeshAgent.SetDestination(worldPosition);
                    }
                }
            }
        }

        public void PickUpObject()
        {
            _isObjectObtained = true;
        }

        public void ObtainObject()
        {
            _isObjectObtained = false;
        }
    }
}