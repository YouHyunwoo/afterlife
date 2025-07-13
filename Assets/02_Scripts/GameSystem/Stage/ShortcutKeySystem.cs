using Afterlife.Core;
using UnityEngine;

namespace Afterlife.GameSystem.Stage
{
    public class ShortcutKeySystem : SystemBase
    {
        [SerializeField] PlayerModeSystem playerModeSystem;
        [SerializeField] ItemUsageSystem itemUsageSystem;
        [SerializeField] CraftSystem craftSystem;
        [SerializeField] UI.Stage.Inventory inventoryView;
        [SerializeField] UI.Stage.Craft craftView;
        [SerializeField] UI.Stage.Menu menuView;

        public void UpdateKeyInput()
        {
            if (Input.GetKeyDown(KeyCode.I))
            {
                if (craftView.IsOpen) { craftView.Hide(); }
                ServiceLocator.Get<AudioManager>().PlaySFX("mouse-click");
                itemUsageSystem.RefreshInventoryView();
                inventoryView.Toggle();
            }
            else if (Input.GetKeyDown(KeyCode.C))
            {
                if (inventoryView.IsOpen) { inventoryView.Hide(); }
                ServiceLocator.Get<AudioManager>().PlaySFX("mouse-click");
                craftSystem.RefreshCraftView();
                craftView.Toggle();
            }
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (inventoryView.IsOpen)
                {
                    ServiceLocator.Get<AudioManager>().PlaySFX("mouse-click");
                    inventoryView.Toggle();
                }
                else if (craftView.IsOpen)
                {
                    ServiceLocator.Get<AudioManager>().PlaySFX("mouse-click");
                    craftView.Toggle();
                }
                else
                {
                    if (playerModeSystem.CurrentMode == EPlayerMode.Interaction)
                    {
                        ServiceLocator.Get<AudioManager>().PlaySFX("mouse-click");
                        menuView.Toggle();
                    }
                }
            }
        } 
    }
}