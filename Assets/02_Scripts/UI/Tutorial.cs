using System;
using Afterlife.Core;
using UnityEngine;

namespace Afterlife.UI
{
    public class Tutorial : View
    {
        [SerializeField] LocalizationManager localizationManager;
        [SerializeField] RectTransform[] highlightAreas;
        [SerializeField] RectTransform[] dialogueAreas;
        [SerializeField] PunchedPatch highlightAreaView;
        [SerializeField] Dialogue dialogueView;
        [SerializeField] string messageKey;
        [SerializeField] int messageCount;

        string[] messages;

        public event Action OnFinishedEvent;

        public void StartTutorial()
        {
            messages = new string[messageCount];
            for (int i = 0; i < messageCount; i++)
            {
                messages[i] = localizationManager[$"{messageKey}.{i}"];
            }

            dialogueView.OnInteractedEvent += OnDialogueInteracted;
            dialogueView.Show();
            dialogueView.SetText(messages);

            ShowTutorial(0);
        }

        void OnDialogueInteracted(string message, int index, int totalCount)
        {
            if (index < totalCount)
            {
                ShowTutorial(index);
            }
            else
            {
                EndTutorial();
            }
        }

        void ShowTutorial(int index)
        {
            if (index < messages.Length)
            {
                dialogueView.SetRectTransform(dialogueAreas[index]);
                highlightAreaView.SetRectTransform(highlightAreas[index]);
            }
            else
            {
                EndTutorial();
            }
        }

        void EndTutorial()
        {
            dialogueView.Hide();
            dialogueView.OnInteractedEvent -= OnDialogueInteracted;
            Hide();
            OnFinishedEvent?.Invoke();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { EndTutorial(); }
        }
    }
}