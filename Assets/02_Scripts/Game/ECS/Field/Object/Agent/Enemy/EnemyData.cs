using UnityEngine;

namespace Afterlife.Dev.Game
{
    [CreateAssetMenu(fileName = "New Enemy", menuName = "Afterlife/EnemyData")]
    public class EnemyData : AgentData
    {
        public float DetectionRange = 3f;
        public float AttackPower = 1f;
        public float AttackRange = 1f;
        public float AttackInterval = 1f;
        public int Aetheron = 1;
    }
}