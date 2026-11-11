using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>A self-contained page view. The owning game decides what each action means.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ThresholdScreenView : MonoBehaviour
    {
        [SerializeField] private ThresholdScreen _screen;
        [SerializeField] private TMP_Text _resultSummary;
        private CanvasGroup _group;
        private readonly List<(UnityEngine.UI.Button button, UnityAction listener)> _bindings = new List<(UnityEngine.UI.Button, UnityAction)>();

        public ThresholdScreen Screen => _screen;
        public event Action<ThresholdAction> ActionRequested;

        public void Configure(ThresholdScreen screen, TMP_Text resultSummary = null)
        {
            _screen = screen;
            _resultSummary = resultSummary;
        }

        private void Awake() => _group = GetComponent<CanvasGroup>();

        private void OnEnable()
        {
            foreach (var action in GetComponentsInChildren<ThresholdActionButton>(true))
            {
                var button = action.GetComponent<UnityEngine.UI.Button>();
                if (button == null) continue;
                var choice = action.Action;
                UnityAction listener = () => ActionRequested?.Invoke(choice);
                button.onClick.AddListener(listener);
                _bindings.Add((button, listener));
            }
        }

        private void OnDisable()
        {
            foreach (var binding in _bindings)
                if (binding.button != null) binding.button.onClick.RemoveListener(binding.listener);
            _bindings.Clear();
        }

        public void Show(bool visible, string summary = null)
        {
            if (_group == null) _group = GetComponent<CanvasGroup>();
            gameObject.SetActive(visible);
            _group.alpha = visible ? 1 : 0;
            _group.interactable = visible;
            _group.blocksRaycasts = visible;
            if (_resultSummary != null) _resultSummary.text = summary ?? string.Empty;
        }
    }
}
