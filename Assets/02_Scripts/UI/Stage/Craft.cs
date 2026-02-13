using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Craft : View
    {
        [SerializeField] ItemInformation itemInformationView;

        public ItemSlot[] ItemSlots;

        public override void Hide()
        {
            base.Hide();
            itemInformationView.Hide();
        }
    }
}