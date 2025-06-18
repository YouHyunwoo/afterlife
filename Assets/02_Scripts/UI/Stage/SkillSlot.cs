using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.UI.Stage
{
    public class SkillSlot : View
    {
        [Header("Icon")]
        public Image IconImage;

        [Header("Cooldown")]
        public Image CooldownImage;

        [Header("Flow")]
        public Image RightFlow;
        public Image DownFlow;
        public Image LeftFlow;
        public Image UpFlow;
        public float FlowSpeed = 1f;
        public float FlowWidth = 3f;

        [Header("Cover")]
        public Button CoverButton;

        public event Action OnSkillSlotClickedEvent;

        Tweener flowTween;
        Sequence flowSequence;

        void Awake()
        {
            CoverButton.onClick.AddListener(OnSkillSlotClicked);
        }

        void OnSkillSlotClicked()
        {
            OnSkillSlotClickedEvent?.Invoke();
        }

        public void SetEnabled(bool enabled)
        {
            IconImage.color = enabled ? Color.white : new Color(1f, 1f, 1f, 0.5f);
        }

        public void SetIcon(Sprite icon) => IconImage.sprite = icon;
        public void SetCooldownRatio(float ratio) => CooldownImage.fillAmount = ratio;
        public void ShowCooldown() => CooldownImage.gameObject.SetActive(true);
        public void HideCooldown() => CooldownImage.gameObject.SetActive(false);

        public void SetFlowSpeed(float speed) => FlowSpeed = speed;
        public void SetFlowWidth(float width) => FlowWidth = width;

        public void StartFlow()
        {
            var flowDuration = 1 / FlowSpeed;
            var size = (transform as RectTransform).sizeDelta;

            RightFlow.rectTransform.sizeDelta = new Vector2(size.x, FlowWidth);
            DownFlow.rectTransform.sizeDelta = new Vector2(FlowWidth, size.y);
            LeftFlow.rectTransform.sizeDelta = new Vector2(size.x, FlowWidth);
            UpFlow.rectTransform.sizeDelta = new Vector2(FlowWidth, size.y);

            RightFlow.rectTransform.anchoredPosition = new Vector2(-size.x, -FlowWidth);
            DownFlow.rectTransform.anchoredPosition = new Vector2(-FlowWidth, size.y);
            LeftFlow.rectTransform.anchoredPosition = new Vector2(size.x, FlowWidth);
            UpFlow.rectTransform.anchoredPosition = new Vector2(FlowWidth, -size.y);

            flowSequence?.Kill();
            flowTween?.Kill();

            flowTween = RightFlow.rectTransform.DOAnchorPosX(0f, flowDuration).SetEase(Ease.Linear).OnComplete(() =>
            {
                flowSequence = DOTween.Sequence()
                    .Append(RightFlow.rectTransform.DOAnchorPosX(size.x, flowDuration).SetEase(Ease.Linear))
                    .Join(DownFlow.rectTransform.DOAnchorPosY(0f, flowDuration).SetEase(Ease.Linear))
                    .Append(RightFlow.rectTransform.DOAnchorPosX(-size.x, 0f))

                    .Append(DownFlow.rectTransform.DOAnchorPosY(-size.y, flowDuration).SetEase(Ease.Linear))
                    .Join(LeftFlow.rectTransform.DOAnchorPosX(0f, flowDuration).SetEase(Ease.Linear))
                    .Append(DownFlow.rectTransform.DOAnchorPosY(size.y, 0f))

                    .Append(LeftFlow.rectTransform.DOAnchorPosX(-size.x, flowDuration).SetEase(Ease.Linear))
                    .Join(UpFlow.rectTransform.DOAnchorPosY(0f, flowDuration).SetEase(Ease.Linear))
                    .Append(LeftFlow.rectTransform.DOAnchorPosX(size.x, 0f))

                    .Append(UpFlow.rectTransform.DOAnchorPosY(size.y, flowDuration).SetEase(Ease.Linear))
                    .Join(RightFlow.rectTransform.DOAnchorPosX(0f, flowDuration).SetEase(Ease.Linear))
                    .Append(UpFlow.rectTransform.DOAnchorPosY(-size.y, 0f))

                    .SetLoops(-1, LoopType.Restart);
            });
        }

        public void StopFlow()
        {
            flowSequence?.Kill();
            flowSequence = null;
            flowTween?.Kill();
            flowTween = null;

            var size = (transform as RectTransform).sizeDelta;
            RightFlow.rectTransform.anchoredPosition = new Vector2(-size.x, -FlowWidth);
            DownFlow.rectTransform.anchoredPosition = new Vector2(-FlowWidth, size.y);
            LeftFlow.rectTransform.anchoredPosition = new Vector2(size.x, FlowWidth);
            UpFlow.rectTransform.anchoredPosition = new Vector2(FlowWidth, -size.y);
        }
    }
}