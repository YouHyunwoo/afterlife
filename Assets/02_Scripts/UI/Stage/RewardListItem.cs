using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class RewardListItem : View
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] Image iconImage;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] Button button;

        public string RewardId;

        public event Action<RewardListItem> OnRewardListItemClickedEvent;

        void Awake()
        {
            button.onClick.AddListener(OnRewardListItemClicked);
        }

        void OnRewardListItemClicked() => OnRewardListItemClickedEvent?.Invoke(this);

        public void SetName(string name) => nameText.text = name;
        public void SetIcon(Sprite icon) => iconImage.sprite = icon;
        public void SetDescription(string description) => descriptionText.text = description;
    }
}