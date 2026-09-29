using Afterlife.Modules.RechargeSlots;
using UnityEngine;

namespace Afterlife.Modules.RechargeSlots.Unity
{
    /// <summary>Place the HUD prefab under a Canvas, then Bind from the game's adapter.</summary>
    public sealed class RechargeSlotsHud : MonoBehaviour
    {
        [SerializeField] private RectTransform[] _fills;
        private RechargeSlotService _service;
        private readonly float[] _blinkUntil = new float[3];
        private bool _subscribed;

        public void Bind(RechargeSlotService service)
        {
            Unsubscribe();
            _service = service;
            System.Array.Clear(_blinkUntil, 0, _blinkUntil.Length);
            if (isActiveAndEnabled) Subscribe();
        }
        private void Awake()
        {
            if (_fills != null && _fills.Length > 0) return;
            var found = new System.Collections.Generic.List<RectTransform>();
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
                if (rect.name == "Fill") found.Add(rect);
            _fills = found.ToArray();
        }
        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
        private void Subscribe()
        {
            if (_service == null || _subscribed) return;
            _service.Recharged += OnRecharged;
            _service.ResetPerformed += ClearBlink;
            _subscribed = true;
        }
        private void Unsubscribe()
        {
            if (!_subscribed || _service == null) return;
            _service.Recharged -= OnRecharged;
            _service.ResetPerformed -= ClearBlink;
            _subscribed = false;
        }
        private void OnRecharged(int index)
        {
            if (index < _blinkUntil.Length) _blinkUntil[index] = Time.unscaledTime + .4f;
        }
        private void ClearBlink() => System.Array.Clear(_blinkUntil, 0, _blinkUntil.Length);
        private void LateUpdate()
        {
            if (_service == null || _fills == null) return;
            for (int i = 0; i < _fills.Length; i++)
            {
                var fill = _fills[i];
                if (fill == null) continue;
                bool visible = i < _service.Slots.Count;
                fill.parent.gameObject.SetActive(visible);
                if (!visible) continue;
                var slot = _service.Slots[i];
                if (!slot.Ready) _blinkUntil[i] = 0;
                float progress = 1 - slot.Remaining / _service.RechargeSeconds;
                fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
                var graphic = fill.GetComponent<UnityEngine.UI.Image>();
                float remaining = _blinkUntil[i] - Time.unscaledTime;
                bool dim = remaining > 0 && Mathf.FloorToInt((.4f - remaining) / .1f) % 2 == 0;
                graphic.color = new Color(1, 1, 1, dim ? .15f : 1);
            }
        }
    }
}
