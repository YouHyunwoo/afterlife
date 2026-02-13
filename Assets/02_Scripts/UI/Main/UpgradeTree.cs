namespace Afterlife.UI.Main
{
    public class UpgradeTree : View
    {
        public UpgradeNode[] upgradeNodes;

        void Awake()
        {
            foreach (var upgradeNode in upgradeNodes)
            {
                upgradeNode.Clear();
            }
        }
    }
}