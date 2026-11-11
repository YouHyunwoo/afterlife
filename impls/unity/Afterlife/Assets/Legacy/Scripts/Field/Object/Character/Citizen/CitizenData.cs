using UnityEngine;

namespace Afterlife.Dev.Field
{
    [CreateAssetMenu(fileName = "New Citizen", menuName = "Afterlife/Citizen")]
    public class CitizenData : ObjectData
    {
        public float AttackPower = 1f;
        public float AttackRange = 1f;
        public float AttackInterval = 1f;
    }
}