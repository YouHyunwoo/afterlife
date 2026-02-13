using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "GameData", menuName = "Afterlife/Data/Game")]
    public class Game : ScriptableObject
    {
        public float Experience;
        public int Lifes;
        public Stage[] StageDataArray;
    }
}