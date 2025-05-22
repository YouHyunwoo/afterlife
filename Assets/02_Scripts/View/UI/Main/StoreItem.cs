using System;
using TMPro;
using UnityEngine.UI;

namespace Afterlife.View
{
    public class StoreItem : UIView
    {
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI DescriptionText;
        public TextMeshProUGUI CostText;
        public Button Button;

        public event Action OnButtonClickedEvent;

        void Awake()
        {
            Button.onClick.AddListener(OnButtonClicked);
        }

        void OnButtonClicked() => OnButtonClickedEvent?.Invoke();

        public void SetName(string name) => NameText.text = name;
        public void SetDescription(string description) => DescriptionText.text = description;
        public void SetCost(string cost) => CostText.text = cost; // 제대로 가로 크기가 표시되는지 확인 필요
    }
}