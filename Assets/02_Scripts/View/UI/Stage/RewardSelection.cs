using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class RewardSelection : UIView
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] Button button;

        public event System.Action OnRewardSelected;

        void Awake()
        {
            button.onClick.AddListener(OnRewardButtonClicked);
        }

        void OnRewardButtonClicked()
        {
            OnRewardSelected?.Invoke();
        }

        public void SetName(string name) => nameText.text = name;
        public void SetDescription(string description) => descriptionText.text = description;
    }
}