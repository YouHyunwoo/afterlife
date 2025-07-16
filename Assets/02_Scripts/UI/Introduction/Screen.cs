using Afterlife.Core;
using TMPro;
using UnityEngine;

namespace Afterlife.UI.Introduction
{
    public class Screen : UI.Screen
    {
        [SerializeField] TextMeshProUGUI messageText;

        [Header("Localization")]
        [SerializeField] TextMeshProUGUI nextKeyText;
        [SerializeField] TextMeshProUGUI skipKeyText;

        public void SetMessage(string message)
        {
            messageText.text = message;
        }

        protected override void OnLocalizationChanged()
        {
            nextKeyText.text = LocalizationManager.Get("introduction.next-key");
            skipKeyText.text = LocalizationManager.Get("introduction.skip-key");
        }
    }
}