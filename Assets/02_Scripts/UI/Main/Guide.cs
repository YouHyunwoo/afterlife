using System.Collections.Generic;
using Afterlife.Core;
using TMPro;

namespace Afterlife.UI.Main
{
    public class Guide : View
    {
        public TextMeshProUGUI GuideText;

        Dictionary<string, string> guideTexts = new Dictionary<string, string>
        {
            { "orb", "main.mission.text" },
            { "magic-circle", "main.upgrade.text" },
            { "opportunity", "main.opportunity.text" },
        };

        public void SetGuideText(string guideId)
        {
            if (guideId == "opportunity")
            {
                GuideText.text = ServiceLocator.Get<LocalizationManager>()[guideTexts[guideId]] + ": " + ServiceLocator.Get<GameManager>().Game.Lives;
                return;
            }

            if (guideTexts.TryGetValue(guideId, out var localizationKey))
            {
                var guideString = ServiceLocator.Get<LocalizationManager>()[localizationKey];
                GuideText.text = guideString;
            }
        }
    }
}