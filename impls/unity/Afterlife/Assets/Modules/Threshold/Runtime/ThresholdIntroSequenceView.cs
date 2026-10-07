using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Five-line sample narration can be replaced through the Inspector without changing flow code.</summary>
    [DisallowMultipleComponent]
    public sealed class ThresholdIntroSequenceView : MonoBehaviour
    {
        [SerializeField] private TMP_Text _subtitle;
        [SerializeField] private AudioSource _voice;
        [SerializeField, TextArea(2, 4)] private string[] _lines;
        [SerializeField] private AudioClip[] _voiceClips;
        [SerializeField] private RectTransform _progressRoot;
        private readonly ThresholdIntroTimeline _timeline = new ThresholdIntroTimeline();
        private UnityEngine.UI.Image[] _fills;
        private bool _running;
        private int _startedFrame;

        public event Action Completed;

        public void Configure(ThresholdIntro source)
        {
            _subtitle = source.Subtitle;
            _voice = source.Voice;
            _lines = source.Lines;
            _voiceClips = source.VoiceClips;
            _progressRoot = source.ProgressRoot;
        }

        public void Begin()
        {
            _timeline.Reset();
            _running = true;
            _startedFrame = Time.frameCount;
            BuildProgress();
            ShowLine();
        }

        public void Stop()
        {
            _running = false;
            if (_voice != null) _voice.Stop();
            if (_subtitle != null) _subtitle.text = string.Empty;
        }

        private void OnDisable() => Stop();

        private void Update()
        {
            if (!_running || Time.frameCount == _startedFrame) return;
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Finish();
                return;
            }
            bool advance = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
            advance |= keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
            if (!advance) return;
            _timeline.Advance(_lines == null ? 0 : _lines.Length);
            if (_timeline.Complete) Finish();
            else ShowLine();
        }

        private void ShowLine()
        {
            if (_lines == null || _lines.Length == 0) { Finish(); return; }
            int index = _timeline.Index;
            if (_subtitle != null) _subtitle.text = _lines[index];
            if (_voice != null)
            {
                _voice.Stop();
                _voice.clip = _voiceClips != null && index < _voiceClips.Length ? _voiceClips[index] : null;
                if (_voice.clip != null) _voice.Play();
            }
            if (_fills != null)
                for (int i = 0; i < _fills.Length; i++) _fills[i].enabled = i > index;
        }

        private void Finish()
        {
            Stop();
            Completed?.Invoke();
        }

        private void BuildProgress()
        {
            if (_progressRoot == null) return;
            if (_fills != null && _fills.Length == (_lines == null ? 0 : _lines.Length))
            {
                bool reusable = true;
                foreach (var fill in _fills) if (fill == null) { reusable = false; break; }
                if (reusable) return;
            }
            for (int i = _progressRoot.childCount - 1; i >= 0; i--)
                Destroy(_progressRoot.GetChild(i).gameObject);
            _fills = new UnityEngine.UI.Image[_lines == null ? 0 : _lines.Length];
            for (int i = 0; i < _fills.Length; i++)
            {
                var box = new GameObject("MessageStep", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var rect = (RectTransform)box.transform;
                rect.SetParent(_progressRoot, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, .5f);
                rect.pivot = new Vector2(0, .5f);
                rect.anchoredPosition = new Vector2(i * 28, 0);
                rect.sizeDelta = new Vector2(16, 16);
                var border = box.GetComponent<UnityEngine.UI.Image>();
                border.color = Color.white;
                border.raycastTarget = false;
                var inner = new GameObject("Fill", typeof(RectTransform), typeof(UnityEngine.UI.Image));
                var inset = (RectTransform)inner.transform;
                inset.SetParent(rect, false);
                inset.anchorMin = Vector2.zero;
                inset.anchorMax = Vector2.one;
                inset.offsetMin = Vector2.one * 2;
                inset.offsetMax = Vector2.one * -2;
                var fill = inner.GetComponent<UnityEngine.UI.Image>();
                fill.color = Color.black;
                fill.raycastTarget = false;
                _fills[i] = fill;
            }
        }
    }
}
