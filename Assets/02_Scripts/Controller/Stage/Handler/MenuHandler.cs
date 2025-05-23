using UnityEngine;

namespace Afterlife.Controller
{
    public class MenuHandler : Handler
    {
        View.StageMenu stageMenuView;

        public MenuHandler(Controller controller) : base(controller)
        {
            stageMenuView = controller.StageView.StageMenuView;
        }

        public override void SetUp()
        {
            stageMenuView.OnMenuButtonClickEvent += OnMenuButtonClicked;
        }

        public override void TearDown()
        {
            stageMenuView.OnMenuButtonClickEvent -= OnMenuButtonClicked;
        }

        void OnMenuButtonClicked()
        {
            var menuView = controller.StageView.StageMenuView.MenuView;
            menuView.SetActive(!menuView.activeSelf);
            Debug.Log("Menu button clicked");
        }
    }
}