namespace Afterlife.Model
{
    [System.Serializable]
    public class Game
    {
        public Data.Game Data;
        public int Lifes;
        public Player Player;
        public Stage Stage;
        public int CurrentStageIndex;
        public int TotalStageCount;
    }
}