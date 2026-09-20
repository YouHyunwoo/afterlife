using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public class StagePlayerBindingSystem
    {
        Model.Player player;

        public void Bind(Model.Stage stage)
        {
            player = ServiceLocator.Get<GameManager>().Game.Player;

            player.OnExperienceChanged += OnExperienceChanged;
            stage.Map.Fog.Lights.Add(player.Light);
        }

        public void Unbind(Model.Stage stage)
        {
            if (player == null) { return; }

            player.OnExperienceChanged -= OnExperienceChanged;
            stage.Map.Fog.Lights.Remove(player.Light);
            player = null;
        }

        void OnExperienceChanged(float experience)
        {
            var stageScreen = ServiceLocator.Get<UI.Stage.Screen>();
            stageScreen.ExperienceView.SetAmount(experience);
        }
    }
}
