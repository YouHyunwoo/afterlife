using UnityEngine;

namespace Afterlife.Dev.Field
{
    [CreateAssetMenu(fileName = "New Enemy", menuName = "Afterlife/Enemy")]
    public class EnemyData : ObjectData
    {
        public float DetectionRange = 3f;
        public float AttackPower = 1f;
        public float AttackRange = 1f;
        public float AttackInterval = 1f;
    }
}