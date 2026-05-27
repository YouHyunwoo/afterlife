using UnityEngine;

namespace Afterlife.Dev.Game
{
    [CreateAssetMenu(fileName = "New Citizen", menuName = "Afterlife/CitizenData")]
    public class CitizenData : AgentData
    {
        public float AttackPower = 1f;
        public float AttackRange = 1f;
        public float AttackInterval = 1f;
        public float DetectionRange = 3f;
    }
}