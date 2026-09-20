namespace Afterlife.Domain.Progression.Upgrade.UI
{
    public class UpgradeTreeView : Afterlife.UI.View
    {
        public UpgradeNodeView[] upgradeNodes;

        void Awake()
        {
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.Clear();
            }
        }
    }
}
