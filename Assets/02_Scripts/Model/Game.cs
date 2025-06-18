namespace Afterlife.Model
{
    [System.Serializable]
    public class Game
    {
        public Data.Game Data;
        public Upgrade Upgrade;
        public int Lifes;
        public Player Player;
        public Stage Stage;
        public int CurrentStageIndex;
        public int TotalStageCount;
    }
}