using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Controller
{
    public class Controller : MonoBehaviour
    {
        public static Controller Instance;

        [Header("Data")]
        public Data.Game GameData;
        public Data.Skill[] SkillDataArray;
        public Data.Store[] StoreDataArray;
        public Dictionary<string, Data.Skill> SkillDataDictionary;

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

        void Awake()
        {
            Instance = this;

            InitializeSkillDataDictionary();
        }

        void InitializeSkillDataDictionary()
        {
            SkillDataDictionary = new();
            foreach (var skill in SkillDataArray)
            {
                SkillDataDictionary.Add(skill.Id, skill);
            }
        }
    }
}