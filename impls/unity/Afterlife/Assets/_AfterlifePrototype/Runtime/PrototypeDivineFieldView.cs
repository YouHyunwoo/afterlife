using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private readonly Dictionary<int, LineRenderer> _divineFieldRings = new Dictionary<int, LineRenderer>();
        private readonly List<int> _expiredFieldRings = new List<int>();
        private TMP_Text _divinePowerLabel;

        private void UpdateDivineFieldVisuals()
        {
            _expiredFieldRings.Clear();
            foreach (var id in _divineFieldRings.Keys) _expiredFieldRings.Add(id);
            foreach (var field in _model.DivineFields.Fields)
            {
                if (!_divineFieldRings.TryGetValue(field.Id, out var ring))
                {
                    ring = Line("Divine field " + field.Id, new Color(.65f, .48f, 1f), .07f);
                    _divineFieldRings.Add(field.Id, ring);
                }
                _expiredFieldRings.Remove(field.Id);
                float life = Mathf.Clamp01(field.Remaining / _model.DivineFields.Duration);
                float pulse = 1 - Mathf.Clamp01(field.TimeUntilPulse(_model.Interval) / _model.Interval);
                ring.widthMultiplier = .045f + .035f * pulse;
                Tint(ring, new Color(.45f + .3f * pulse, .35f + .25f * life, 1f, .25f + .7f * life));
                Circle(ring, new Point(field.X, field.Z), field.Radius);
            }
            foreach (var id in _expiredFieldRings)
            {
                if (_divineFieldRings.TryGetValue(id, out var ring) && ring != null) Destroy(ring.gameObject);
                _divineFieldRings.Remove(id);
            }
        }

        private void ClearDivineFieldVisuals()
        {
            foreach (var ring in _divineFieldRings.Values) if (ring != null) Destroy(ring.gameObject);
            _divineFieldRings.Clear();
            _expiredFieldRings.Clear();
        }

        private void InitializeDivinePowerHud()
        {
            if (_sceneUi?.Canvas == null) return;
            var go = new GameObject("Divine Power", typeof(RectTransform), typeof(TextMeshProUGUI));
            var rect = (RectTransform)go.transform;
            rect.SetParent(_sceneUi.Canvas.transform, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(18, -88);
            rect.sizeDelta = new Vector2(360, 46);
            _divinePowerLabel = go.GetComponent<TextMeshProUGUI>();
            _divinePowerLabel.font = _sceneUi.ResourcesTmpFont != null ? _sceneUi.ResourcesTmpFont : TMP_Settings.defaultFontAsset;
            _divinePowerLabel.fontSize = 15;
            _divinePowerLabel.color = new Color(.73f, .62f, 1f);
            _divinePowerLabel.raycastTarget = false;
            _divinePowerLabel.text = "";
        }

        private void UpdateDivinePowerHud()
        {
            if (_divinePowerLabel == null) return;
            var fields = _model.DivineFields;
            string value = $"신력 {_model.DivinePower:0}/{fields.MaximumPower:0}  ·  필드 {fields.Fields.Count}  ·  설치 {fields.PlacementCost:0}\n대상 클릭 / 빈 타일 Shift+클릭";
            if (_divinePowerLabel.text != value) _divinePowerLabel.text = value;
        }
    }
}
