using Afterlife.Core;

namespace Afterlife.GameSystem.Stage
{
    public class FogSystem : SystemBase
    {
        Model.Fog fog;

        void LateUpdate()
        {
            fog.Update();
        }

        public override void SetUp()
        {
            fog = ServiceLocator.Get<StageManager>().Stage.Map.Fog;

            enabled = true;
        }

        public override void TearDown()
        {
            enabled = false;

            fog = null;
        }
    }
}