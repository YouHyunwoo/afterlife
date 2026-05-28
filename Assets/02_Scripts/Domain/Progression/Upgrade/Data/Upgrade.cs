using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "UpgradeData", menuName = "Afterlife/Data/Upgrade")]
    public class Upgrade : ScriptableObject
    {
        public string Id;
        public Sprite Icon;
        public int Cost;
        public UpgradeParameter[] Parameters;
    }

    [System.Serializable]
    public class UpgradeParameter
    {
        public string Name;
        public string[] Values;
    }
}