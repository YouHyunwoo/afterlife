using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Afterlife/Data/Game")]
    public class Game : ScriptableObject
    {
        public float Experience;
        public float BaseLevelExperience = 100f;
        public float LevelExperienceGrowth = 1.2f;
        public int Lifes;
        public Stage[] StageDataArray;
    }
}
