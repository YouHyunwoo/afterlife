using UnityEngine;

namespace Afterlife.Controller
{
    public class Controller : MonoBehaviour
    {
        public static Controller Instance;

        [Header("Data")]
        public Data.Game GameData;

        [Header("Controller")]
        public TitleSceneController TitleSceneController;
        public MainSceneController MainSceneController;
        public StageSceneController StageSceneController;

        [Header("View")]
        public View.Title TitleView;
        public View.Main MainView;
        public View.Stage StageView;
        public View.Demo DemoView;
        public View.GameOver GameOverView;

        [Header("Model")]
        public Model.Game Game;

        void Awake() { Instance = this; }
    }
}