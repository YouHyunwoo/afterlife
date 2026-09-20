using UnityEngine;

namespace Afterlife.Data
{
    public enum RewardType
    {
        Day,
        Stage,
        Achievement,
    }

    [CreateAssetMenu(fileName = "RewardData", menuName = "Afterlife/Data/Reward")]
    public class Reward : ScriptableObject
    {
        public string Id;
        public Sprite Icon;
        public RewardType Type;
    }
}