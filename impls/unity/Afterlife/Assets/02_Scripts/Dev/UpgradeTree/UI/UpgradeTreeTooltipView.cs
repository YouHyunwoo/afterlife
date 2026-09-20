using TMPro;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.Dev.UpgradeTree.UI
{
    public class UpgradeTreeTooltipView : Afterlife.UI.View
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private TextMeshProUGUI _descriptionText;

        public void Show(string name, int acquiredLevelCount, int maxLevelCount, int cost, string description, bool completed)
        {
            if (_nameText != null) { _nameText.text = name; }
            if (_levelText != null) { _levelText.text = $"{acquiredLevelCount}/{maxLevelCount}"; }
            if (_costText != null) { _costText.text = completed ? "완료" : $"<sprite=0>{cost}"; }
            if (_descriptionText != null) { _descriptionText.text = ResolveDescription(description); }

            Show();
        }

        private string ResolveDescription(string description)
        {
            if (string.IsNullOrEmpty(description)) { return string.Empty; }

            try
            {
                var localizedText = LocalizationManager.Get(description);
                return localizedText.StartsWith("<Missing:") ? description : localizedText;
            }
            catch (System.Exception)
            {
                return description;
            }
        }
    }
}
