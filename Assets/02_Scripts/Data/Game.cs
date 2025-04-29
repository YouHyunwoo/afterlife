using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Afterlife/Data/Game")]
    public class Game : ScriptableObject
    {
        public int Lifes;
        public int TotalStageCount;
        public Stage stageData;
    }
}