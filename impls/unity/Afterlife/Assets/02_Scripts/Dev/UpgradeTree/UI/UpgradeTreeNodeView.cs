using System;
using Afterlife.Dev.UpgradeTree.Data;
using Afterlife.Dev.UpgradeTree.Model;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.Dev.UpgradeTree.UI
{
    public class UpgradeTreeNodeView : Afterlife.UI.View, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly int FillShaderPropertyId = Shader.PropertyToID("_Fill");

        [Header("Controls")]
        [SerializeField] private Button _button;
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _fillImage;
        [SerializeField] private Image _lockedOverlayImage;
        [SerializeField] private Image _borderImage;

        [Header("Colors")]
        [SerializeField] private Color _availableColor = new(0.75f, 0.75f, 0.75f, 1f);
        [SerializeField] private Color _completedColor = Color.white;

        private Material _fillMaterialInstance;

        public UpgradeTreeNodeData NodeData { get; private set; }
        public UpgradeTreeNodeState State { get; private set; }

        public event Action<UpgradeTreeNodeView> OnClicked;
        public event Action<UpgradeTreeNodeView> OnInformationShowed;
        public event Action<UpgradeTreeNodeView> OnInformationHidden;

        private void Awake()
        {
            InitializeFillMaterial();
            InitializeLockedOverlay();

            if (_button != null)
            {
                _button.onClick.AddListener(HandleButtonClick);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleButtonClick);
            }

            if (_fillMaterialInstance != null)
            {
                Destroy(_fillMaterialInstance);
                _fillMaterialInstance = null;
            }
        }

        public void Bind(UpgradeTreeNodeData nodeData)
        {
            NodeData = nodeData;
            if (_iconImage != null)
            {
                _iconImage.sprite = nodeData != null ? nodeData.Icon : null;
                _iconImage.enabled = _iconImage.sprite != null;
            }
        }

        public void Refresh(
            UpgradeTreeNodeState state,
            int acquiredLevelCount,
            int maxLevelCount,
            bool canAffordNextLevel)
        {
            State = state;
            gameObject.SetActive(state != UpgradeTreeNodeState.Hidden);

            SetFillAmount(CalculateFillAmount(acquiredLevelCount, maxLevelCount));
            SetLockedOverlayVisible(state == UpgradeTreeNodeState.Available && !canAffordNextLevel);

            if (_borderImage != null)
            {
                _borderImage.color = state == UpgradeTreeNodeState.Completed ? _completedColor : _availableColor;
            }

            if (_button != null)
            {
                _button.interactable = state == UpgradeTreeNodeState.Available;
            }
        }

        private void HandleButtonClick()
        {
            if (State == UpgradeTreeNodeState.Available)
            {
                OnClicked?.Invoke(this);
            }
        }

        void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
        {
            if (State != UpgradeTreeNodeState.Hidden)
            {
                OnInformationShowed?.Invoke(this);
            }
        }

        void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
        {
            OnInformationHidden?.Invoke(this);
        }

        private void InitializeFillMaterial()
        {
            if (_fillImage == null)
            {
                _fillImage = FindChildImage("Fill Image");
            }

            if (_fillImage == null || _fillImage.material == null) { return; }

            _fillMaterialInstance = new Material(_fillImage.material)
            {
                name = $"{_fillImage.material.name} (Instance)"
            };
            _fillImage.material = _fillMaterialInstance;
            SetFillAmount(0f);
        }

        private void InitializeLockedOverlay()
        {
            if (_lockedOverlayImage == null)
            {
                _lockedOverlayImage = FindChildImage("Locked Overlay Image");
            }

            if (_lockedOverlayImage == null) { return; }

            SetLockedOverlayVisible(false);
        }

        private Image FindChildImage(string childName)
        {
            var images = GetComponentsInChildren<Image>(true);
            foreach (var image in images)
            {
                if (image != null && image.name == childName)
                {
                    return image;
                }
            }

            return null;
        }

        private float CalculateFillAmount(int acquiredLevelCount, int maxLevelCount)
        {
            if (maxLevelCount <= 0) { return 1f; }
            return Mathf.Clamp01((float)acquiredLevelCount / maxLevelCount);
        }

        private void SetLockedOverlayVisible(bool visible)
        {
            if (_lockedOverlayImage != null)
            {
                _lockedOverlayImage.gameObject.SetActive(visible);
            }
        }

        private void SetFillAmount(float fillAmount)
        {
            if (_fillMaterialInstance == null || !_fillMaterialInstance.HasProperty(FillShaderPropertyId)) { return; }

            _fillMaterialInstance.SetFloat(FillShaderPropertyId, fillAmount);
        }
    }
}
