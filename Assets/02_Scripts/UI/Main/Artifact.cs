using System;
using TMPro;
using UnityEngine;

namespace Afterlife.UI.Main
{
    public class Artifact : View
    {
        [SerializeField] TextMeshProUGUI availableSelectionCountText;

        public ArtifactListItem[] ArtifactListItems;

        public event Action<ArtifactListItem> OnArtifactListItemClickedEvent;

        void Awake()
        {
            if (ArtifactListItems == null) { return; }

            foreach (var item in ArtifactListItems)
            {
                if (item == null) { continue; }
                item.OnArtifactListItemClickedEvent += OnArtifactListItemClicked;
            }
        }

        void OnArtifactListItemClicked(ArtifactListItem item)
        {
            OnArtifactListItemClickedEvent?.Invoke(item);
        }

        public void SetAvailableSelectionCount(int count)
        {
            if (availableSelectionCountText == null) { return; }

            availableSelectionCountText.text = $"{count}";
        }

        public void SetArtifactList(Data.Artifact[] artifactCandidates)
        {
            if (ArtifactListItems == null) { return; }

            for (int i = 0; i < ArtifactListItems.Length; i++)
            {
                var item = ArtifactListItems[i];
                if (item == null) { continue; }
                if (i >= artifactCandidates.Length)
                {
                    item.gameObject.SetActive(false);
                    continue;
                }

                var artifact = artifactCandidates[i];
                item.ArtifactId = artifact.Id;
                item.SetName(Core.LocalizationManager.Get($"artifact.{artifact.Id}.name"));
                item.SetDescription(Core.LocalizationManager.Get($"artifact.{artifact.Id}.description"));
                item.SetIcon(artifact.Icon);
                item.gameObject.SetActive(true);
            }
        }
    }
}
