using UnityEngine;

namespace Afterlife.Dev.Game
{
    [CreateAssetMenu(fileName = "New Agent", menuName = "Afterlife/AgentData")]
    public class AgentData : ObjectData
    {
        public float Health;
        public float MoveSpeed;
    }
}