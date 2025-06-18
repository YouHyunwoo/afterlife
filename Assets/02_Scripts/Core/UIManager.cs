using UnityEngine;

namespace Afterlife.Core
{
    /// <summary>
    /// 게임 내 모든 UI 화면을 전환 및 관리하는 UIManager
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Screens")]
        public UI.Screen TitleScreen;
        public UI.Screen MainScreen;
        public UI.Screen InGameScreen;
        public UI.Screen ClearScreen; // 클리어 화면
        public UI.Screen GameOverScreen; // 게임 오버 화면

        [Header("Controllers")]
        public UI.Title.Controller TitleController;
        public UI.Main.Controller MainController;
        public UI.Stage.Controller StageController;

        public void Show(GameState state)
        {
            HideAll();
            switch (state)
            {
                case GameState.Title:
                    TitleScreen.Show();
                    break;
                case GameState.Main:
                    MainScreen.Show();
                    break;
                case GameState.InGame:
                    InGameScreen.Show();
                    break;
                case GameState.Clear:
                    ClearScreen.Show();
                    break;
                case GameState.GameOver:
                    GameOverScreen.Show();
                    break;
            }
        }

        private void HideAll()
        {
            TitleScreen.Hide();
            MainScreen.Hide();
            InGameScreen.Hide();
            ClearScreen.Hide();
            GameOverScreen.Hide();
        }
    }
}
