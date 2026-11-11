using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Unity
{
    /// <summary>Click/Enter advances one line; Escape skips the introduction.</summary>
    public sealed class ThresholdIntro : ThresholdPart
    {
        public TMP_Text Subtitle;
        public AudioSource Voice;
        [TextArea(2, 4)] public string[] Lines;
        public AudioClip[] VoiceClips;
        public RectTransform ProgressRoot;
        private readonly ThresholdIntroTimeline _timeline = new ThresholdIntroTimeline();
        private Image[] _fills;
        private bool _active, _paused;
        private int _shown = -1;
        private void Update()
        {
            var session = Host != null ? Host.Session : null;
            bool active = session != null && session.Current == ThresholdScreen.Lobby && !session.IsMoving;
            if (!active) { if (_active) Stop(); return; }
            if (!_active)
            {
                _active = true; _timeline.Reset(); _shown = -1;
                BuildProgress(); ShowLine();
                return; // The click that entered the intro must not advance its first line.
            }
            if (session.ModalOpen)
            {
                if (!_paused && Voice != null) Voice.Pause();
                _paused = true; return;
            }
            if (_paused) { if (Voice != null) Voice.UnPause(); _paused = false; return; }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            { Host.Dispatch(ThresholdAction.EnterTrial); Stop(); return; }
            bool next = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame));
            if (!next) return;
            _timeline.Advance(Lines == null ? 0 : Lines.Length);
            if (_timeline.Complete) { Host.Dispatch(ThresholdAction.EnterTrial); Stop(); return; }
            ShowLine();
        }
        private void ShowLine()
        {
            if (Lines == null || Lines.Length == 0) { Host.Dispatch(ThresholdAction.EnterTrial); Stop(); return; }
            _shown = _timeline.Index;
            if (Subtitle != null) Subtitle.text = Lines[_shown];
            if (Voice != null)
            {
                Voice.Stop();
                if (VoiceClips != null && _shown < VoiceClips.Length && VoiceClips[_shown] != null)
                { Voice.clip = VoiceClips[_shown]; Voice.Play(); }
            }
            if (_fills != null)
                for (int i = 0; i < _fills.Length; i++) _fills[i].enabled = i > _shown;
        }
        private void BuildProgress()
        {
            if (ProgressRoot == null) return;
            foreach (Transform child in ProgressRoot) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            _fills = new Image[Lines == null ? 0 : Lines.Length];
            for (int i = 0; i < _fills.Length; i++)
            {
                var box = new GameObject("MessageStep", typeof(RectTransform), typeof(Image));
                var rect = (RectTransform)box.transform; rect.SetParent(ProgressRoot, false);
                rect.anchorMin = rect.anchorMax = new Vector2(0, .5f); rect.pivot = new Vector2(0, .5f);
                rect.anchoredPosition = new Vector2(i * 28, 0); rect.sizeDelta = new Vector2(16, 16);
                box.GetComponent<Image>().color = Color.white; box.GetComponent<Image>().raycastTarget = false;
                var inside = new GameObject("Fill", typeof(RectTransform), typeof(Image));
                var inner = (RectTransform)inside.transform; inner.SetParent(rect, false);
                inner.anchorMin = Vector2.zero; inner.anchorMax = Vector2.one;
                inner.offsetMin = Vector2.one * 2; inner.offsetMax = Vector2.one * -2;
                inside.GetComponent<Image>().color = Color.black; inside.GetComponent<Image>().raycastTarget = false;
                // Black inset punches a hollow square; hiding it fills the current/completed step.
                _fills[i] = inside.GetComponent<Image>();
            }
        }
        private void Stop()
        {
            if (Voice != null) Voice.Stop();
            if (Subtitle != null) Subtitle.text = "";
            _active = false; _paused = false; _shown = -1;
        }
        protected override void OnDisable() { Stop(); base.OnDisable(); }
    }
}
