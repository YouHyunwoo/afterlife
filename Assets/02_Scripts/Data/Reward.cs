using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "RewardData", menuName = "Afterlife/Data/Reward")]
    public class Reward : ScriptableObject
    {
        public string Id;
        public string Name;
        public string Description;
    }
}