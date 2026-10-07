using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Prototype
{
    public readonly struct PrototypeUiPreferenceValues
    {
        public readonly float Size;
        public readonly float Edge;
        public readonly float Speed;
        public PrototypeUiPreferenceValues(float size, float edge, float speed)
        { Size = size; Edge = edge; Speed = speed; }
    }

    public static class PrototypeUiPreferences
    {
        public static PrototypeUiPreferenceValues Load(float defaultSize = 1, float defaultEdge = .25f, float defaultSpeed = 1)
            => new PrototypeUiPreferenceValues(
                PlayerPrefs.GetFloat("Afterlife.UI.Size", defaultSize),
                PlayerPrefs.GetFloat("Afterlife.Camera.Edge", defaultEdge),
                PlayerPrefs.GetFloat("Afterlife.Camera.Speed", defaultSpeed));

        public static void Save(PrototypeUiPreferenceValues values)
        {
            PlayerPrefs.SetFloat("Afterlife.UI.Size", values.Size);
            PlayerPrefs.SetFloat("Afterlife.Camera.Edge", values.Edge);
            PlayerPrefs.SetFloat("Afterlife.Camera.Speed", values.Speed);
        }
    }

    /// <summary>Reuse the prototype settings content on the title Canvas without enabling gameplay UI.</summary>
    [DisallowMultipleComponent]
    public sealed class PrototypeTitleSettings : MonoBehaviour
    {
        private UnityEngine.UI.Slider _size;
        private UnityEngine.UI.Slider _edge;
        private UnityEngine.UI.Slider _speed;
        private UnityEngine.UI.Button _close;
        private UnityEngine.UI.Button[] _tabs;
        private TMP_Text _sizeLabel;
        private TMP_Text _edgeLabel;
        private TMP_Text _speedLabel;
        private GameObject[] _pages;
        private bool _bound;

        public event Action Closed;

        private void Awake()
        {
            _size = Find<UnityEngine.UI.Slider>("Menu.Size");
            _edge = Find<UnityEngine.UI.Slider>("Menu.Edge");
            _speed = Find<UnityEngine.UI.Slider>("Menu.Speed");
            _close = Find<UnityEngine.UI.Button>("Settings.Close");
            _tabs = new[] { Find<UnityEngine.UI.Button>("Settings.Tab.0"), Find<UnityEngine.UI.Button>("Settings.Tab.1") };
            _sizeLabel = Find<TMP_Text>("Menu.Size.Label");
            _edgeLabel = Find<TMP_Text>("Menu.Edge.Label");
            _speedLabel = Find<TMP_Text>("Menu.Speed.Label");
            var pages = transform.Find("Tab Pages");
            _pages = new[] { pages != null ? pages.Find("Display")?.gameObject : null, pages != null ? pages.Find("Controls")?.gameObject : null };
            AddInputShield();
            if (_size == null || _edge == null || _speed == null || _close == null || _tabs[0] == null || _tabs[1] == null)
            { Debug.LogError("Title settings prefab is missing a control.", this); return; }
            _size.onValueChanged.AddListener(_ => Save());
            _edge.onValueChanged.AddListener(_ => Save());
            _speed.onValueChanged.AddListener(_ => Save());
            _close.onClick.AddListener(Close);
            _tabs[0].onClick.AddListener(ShowDisplay);
            _tabs[1].onClick.AddListener(ShowControls);
            _bound = true;
        }

        private T Find<T>(string objectName) where T : Component
        {
            foreach (var item in GetComponentsInChildren<T>(true)) if (item.name == objectName) return item;
            return null;
        }

        private void AddInputShield()
        {
            if (transform.Find("InputShield") != null) return;
            var shield = new GameObject("InputShield", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            var rect = (RectTransform)shield.transform;
            rect.SetParent(transform, false);
            rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = shield.GetComponent<UnityEngine.UI.Image>();
            image.color = Color.clear;
            image.raycastTarget = true;
        }

        public void Show()
        {
            gameObject.SetActive(true);
            var values = PrototypeUiPreferences.Load();
            if (_size != null) _size.SetValueWithoutNotify(values.Size);
            if (_edge != null) _edge.SetValueWithoutNotify(values.Edge);
            if (_speed != null) _speed.SetValueWithoutNotify(values.Speed);
            RefreshLabels();
            SetTab(0);
        }

        public void Hide() => gameObject.SetActive(false);

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) Close();
        }

        private void Close() { Hide(); Closed?.Invoke(); }
        private void ShowDisplay() => SetTab(0);
        private void ShowControls() => SetTab(1);

        private void SetTab(int tab)
        {
            for (int i = 0; i < _pages.Length; i++) if (_pages[i] != null) _pages[i].SetActive(i == tab);
            for (int i = 0; i < _tabs.Length; i++)
                if (_tabs[i] != null && _tabs[i].targetGraphic != null)
                    _tabs[i].targetGraphic.color = i == tab ? new Color(.12f, .26f, .30f) : new Color(.09f, .14f, .20f);
        }

        private void Save()
        {
            if (_size == null || _edge == null || _speed == null) return;
            PrototypeUiPreferences.Save(new PrototypeUiPreferenceValues(_size.value, _edge.value, _speed.value));
            RefreshLabels();
        }

        private void RefreshLabels()
        {
            if (_sizeLabel != null) _sizeLabel.text = $"UI 크기 {_size.value * 100:0}%";
            if (_edgeLabel != null) _edgeLabel.text = $"카메라 이동 구간 {_edge.value * 100:0}%";
            if (_speedLabel != null) _speedLabel.text = $"카메라 속도 {_speed.value:0.0}배";
        }

        private void OnDestroy()
        {
            if (!_bound) return;
            _size.onValueChanged.RemoveAllListeners();
            _edge.onValueChanged.RemoveAllListeners();
            _speed.onValueChanged.RemoveAllListeners();
            _close.onClick.RemoveListener(Close);
            _tabs[0].onClick.RemoveListener(ShowDisplay);
            _tabs[1].onClick.RemoveListener(ShowControls);
        }
    }
}
