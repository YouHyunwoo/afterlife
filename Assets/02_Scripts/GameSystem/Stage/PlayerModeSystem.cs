using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public enum EPlayerMode
    {
        None,
        Interaction,
        Construction,
    }

    public class PlayerModeSystem : SystemBase
    {
        public EPlayerMode CurrentMode;

        public override void SetUp()
        {
            CurrentMode = EPlayerMode.Interaction;
        }

        public override void TearDown()
        {
            CurrentMode = EPlayerMode.None;
        }

        public void SetMode(EPlayerMode mode)
        {
            CurrentMode = mode;
        }
    }
}