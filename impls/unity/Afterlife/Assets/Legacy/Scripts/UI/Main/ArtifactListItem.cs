using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Main
{
    public class ArtifactListItem : View
    {
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] Image iconImage;
        [SerializeField] Button button;

        public string ArtifactId;

        public event Action<ArtifactListItem> OnArtifactListItemClickedEvent;

        void Awake()
        {
            button.onClick.AddListener(OnButtonClicked);
        }

        void OnButtonClicked()
        {
            OnArtifactListItemClickedEvent?.Invoke(this);
        }

        public void SetName(string name) => nameText.text = name;
        public void SetDescription(string description) => descriptionText.text = description;

        public void SetIcon(Sprite icon)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }
    }
}
