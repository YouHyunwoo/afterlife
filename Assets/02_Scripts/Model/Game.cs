using Afterlife.Domain.Progression.Upgrade.Model;

namespace Afterlife.Model
{
    [System.Serializable]
    public class Game
    {
        public Data.Game Data;
        public bool IsMainTutorialActive;
        public bool IsStageTutorialActive;
        public UpgradeService Upgrade;
        public Artifact Artifact;
        public Reward Reward;
        public int Lives;
        public Player Player;
        public Stage Stage;
        public int CurrentStageIndex;
        public int TotalStageCount;
    }
}
