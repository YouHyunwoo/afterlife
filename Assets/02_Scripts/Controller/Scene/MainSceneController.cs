using UnityEngine;

namespace Afterlife.Controller
{
    public class MainSceneController : MonoBehaviour
    {
        [Header("Controller")]
        public StageSceneController StageSceneController;

        [Header("View")]
        public View.Title TitleView;
        public View.Main MainView;
        public View.Stage StageView;

        public Model.Game Game;

        void Awake()
        {
            MainView.PowerView.OnButtonClickedEvent += OnPowerButtonClicked;
        }

        void OnDestroy()
        {
            MainView.PowerView.OnButtonClickedEvent -= OnPowerButtonClicked;
        }

        void OnPowerButtonClicked(string category, int index)
        {
            if (category == "player-statistics")
            {
                switch (index)
                {
                    case 0:
                        Debug.Log("Player Attack Power Increased");
                        if (Game.Player.Experience < 2) {
                            Debug.LogError("Not enough experience to increase attack power.");
                            return;
                        }
                        else {
                            Game.Player.Experience -= 2;
                            Game.Player.AttackPower += 1;
                            Debug.Log($"Player Experience: {Game.Player.Experience}");
                            Debug.Log($"Player Attack Power: {Game.Player.AttackPower}");
                            MainView.PowerView.ExperienceView.SetExperience(Game.Player.Experience);
                        }
                        break;
                    case 1:
                        Debug.Log("Player Attack Speed Increased");
                        Game.Player.AttackSpeed += 0.02f;
                        break;
                    case 2:
                        break;
                    case 3:
                        break;
                    default:
                        Debug.LogError($"Invalid index: {index}");
                        break;
                }
            }
        }

        public void Initialize(Model.Game game)
        {
            Game = game;
            MainView.SetLifes(game.Lifes);
            MainView.SetStageProgress(game.CurrentStageIndex, game.TotalStageCount);
        }

        public void StartMission()
        {
            MainView.Hide();
            StageView.Show();
            StageSceneController.StartStage(Game);
        }

        public void QuitGame()
        {
            TitleView.Show();
            MainView.Hide();
        }
    }
}