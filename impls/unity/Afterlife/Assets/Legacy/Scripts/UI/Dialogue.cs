using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI
{
    public class Dialogue : View
    {
        [SerializeField] Image portraitImage;
        [SerializeField] Button interactionButton;
        [SerializeField] TextMeshProUGUI dialogueText;
        [SerializeField] Image interactionIconImage;
        [SerializeField] KeyCode interactionKey = KeyCode.Space;
        [SerializeField] bool interactable = true;

        RectTransform rectTransform;
        Coroutine currentCoroutine;
        bool isAnimating;
        bool isCompleteRequested;

        string[] dialogueHistory;
        int currentDialogueIndex = 0;

        public event Action<string, int, int> OnInteractedEvent;

        void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            interactionButton.onClick.AddListener(OnInteracted);
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            interactionButton.interactable = value;
            enabled = value;
        }

        public void Interact()
        {
            if (interactable)
            {
                interactionButton.onClick.Invoke();
            }
        }

        void OnInteracted()
        {
            if (isAnimating)
            {
                isCompleteRequested = true;
            }
            else
            {
                var isEnd = NextText();
                OnInteractedEvent?.Invoke(isEnd ? null : dialogueHistory[currentDialogueIndex], currentDialogueIndex, dialogueHistory.Length);
            }
        }

        public void SetRectTransform(RectTransform rectTransform)
        {
            this.rectTransform.pivot = rectTransform.pivot;
            this.rectTransform.anchorMin = rectTransform.anchorMin;
            this.rectTransform.anchorMax = rectTransform.anchorMax;
            this.rectTransform.anchoredPosition = rectTransform.anchoredPosition;
            this.rectTransform.sizeDelta = rectTransform.sizeDelta;
            this.rectTransform.localScale = rectTransform.localScale;
        }
        public void SetPosition(Vector2 position) => rectTransform.anchoredPosition = position;
        public void SetSize(Vector2 size) => rectTransform.sizeDelta = size;
        public void SetPortrait(Sprite portrait) => portraitImage.sprite = portrait;
        public void SetText(string[] messages) => ProcessText(messages);
        public void SetInteractionIcon(Sprite icon) => interactionIconImage.sprite = icon;

        void ProcessText(string[] messages)
        {
            dialogueHistory = messages;
            currentDialogueIndex = 0;
            isAnimating = false;
            isCompleteRequested = false;
            AnimateText(messages[0]);
        }

        bool NextText()
        {
            currentDialogueIndex = Mathf.Min(currentDialogueIndex + 1, dialogueHistory.Length);
            if (currentDialogueIndex < dialogueHistory.Length)
            {
                AnimateText(dialogueHistory[currentDialogueIndex]);
                return false;
            }
            else
            {
                return true;
            }
        }

        void AnimateText(string message)
        {
            if (currentCoroutine != null)
            {
                StopCoroutine(currentCoroutine);
            }
            currentCoroutine = StartCoroutine(TypeText(message));
        }

        IEnumerator TypeText(string message)
        {
            isAnimating = true;
            isCompleteRequested = false;
            interactionIconImage.gameObject.SetActive(false);
            dialogueText.text = "";

            int length = message.Length;
            for (int i = 0; i < length; i++)
            {
                if (isCompleteRequested) { break; }

                if (message[i] == '\\' && i + 1 < length && message[i + 1] == 'n')
                {
                    dialogueText.text += '\n';
                    i++;
                }
                else
                {
                    dialogueText.text += message[i];
                }
                yield return new WaitForSeconds(0.05f);
            }

            isAnimating = false;
            dialogueText.text = message;
            interactionIconImage.gameObject.SetActive(true);
            currentCoroutine = null;
        }

        void Update()
        {
            if (Input.GetKeyDown(interactionKey)) { Interact(); }
        }
    }
}