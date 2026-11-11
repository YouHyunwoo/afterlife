using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Modules.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        public ProgressionCatalog ProgressionCatalog { get; set; }
        [Header("진행 UI · 씬 프리팹 연결")]
        [SerializeField] private PrototypeModalPanel _rebirthReviewPanel;
        [SerializeField] private UpgradeTreeScreen _researchTreeScreen, _altarTreeScreen;
        [SerializeField] private RectTransform _skillBar;
        [SerializeField] private Button _lightningButton, _abundanceButton, _ritualButton;
        private readonly Dictionary<string, Button> _skillButtons = new Dictionary<string, Button>();
        private Point _skillAim;
        private bool _skillAimValid;
        private readonly List<(LineRenderer Line, float Expires)> _skillBolts = new List<(LineRenderer, float)>();

        private void PrepareProgressionUi()
        {
            if (ProgressionCatalog == null || _model.Upgrades == null) return;
            if (_model.Settings.MvpSlots) return;
            if (_rebirthReviewPanel == null) { Debug.LogError("Assign RebirthReview panel in the scene.", this); return; }
            _rebirthReviewPanel.Connect(_sceneUi);
        }
        private void InitializeProgressionUi()
        {
            if (ProgressionCatalog == null || _model.Upgrades == null) return;
            foreach (var tree in ProgressionCatalog.Trees)
            {
                var modal = tree.Id == "research" ? Modal.Research : Modal.Altar;
                var root = _sceneUi.Item("Modal." + modal)?.Rect; if (root == null) continue;
                foreach (Transform child in root) child.gameObject.SetActive(false);
                var surface = root.GetComponent<PrototypeModalSurface>();
                if (surface == null) throw new InvalidOperationException("Research and Altar panels need PrototypeModalSurface in the scene.");
                surface.Presentation = ModalPresentation.Screen;
                if (tree.Id == "altar")
                {
                    // 화면 뒤의 암전/블러는 Modal Surface 설정에 맡긴다.
                    var panelImage = root.GetComponent<Image>();
                    if (panelImage != null) panelImage.enabled = false;
                }
                UpgradeTreeScreen.Stretch(root);
                var screen = tree.Id == "research" ? _researchTreeScreen : _altarTreeScreen;
                if (screen == null) { Debug.LogError("Assign the UpgradeTreeScreen scene instances.", this); continue; }
                screen.gameObject.SetActive(true);
                string treeId = tree.Id;
                Action extra = treeId == "research" && !_model.Settings.MvpSlots
                    ? (Action)(() => PushModal(Modal.Altar))
                    : treeId == "altar" && !_model.Settings.MvpSlots
                        ? (Action)(() => PushModal(Modal.RebirthReview)) : null;
                screen.Bind(tree, _model.Upgrades, _sceneUi.ResourcesTmpFont, _model.BuyProgression, () => _model.CanUseTree(treeId), () => _model.Etheron, CloseModal,
                    extra, () => !_model.Ended && _model.HasAltar,
                    node => Resources.Load<Sprite>("UI/Upgrades/" + node.Id), _model.CanUseProgressionNode,
                    treeId == "research" ? "마법 강화" : null);
            }
            if (_skillBar == null || _lightningButton == null || _abundanceButton == null || _ritualButton == null)
            { Debug.LogError("Assign AltarSkillBar and its three prefab buttons.", this); return; }
            _skillButtons.Add("lightning", _lightningButton); _lightningButton.onClick.AddListener(() => CastSkill("lightning"));
            _skillButtons.Add("abundance", _abundanceButton); _abundanceButton.onClick.AddListener(() => CastSkill("abundance"));
            _skillButtons.Add("ritual", _ritualButton); _ritualButton.onClick.AddListener(() => CastSkill("ritual"));
            _model.LightningStruck += ShowSkillLightning;
        }
        private void UpdateProgressionInput()
        {
            if (_model.Skills == null) return;
            if (_pointerValid && _model.PointerVisible && _modal == Modal.None) { _skillAim = _pointer; _skillAimValid = true; }
            if (!_model.Settings.MvpSlots && _modal == Modal.None && !_model.Frozen && !_tileBuildMenu && Application.isFocused && Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) CastSkill("lightning");
                if (Keyboard.current.wKey.wasPressedThisFrame) CastSkill("abundance");
                if (Keyboard.current.eKey.wasPressedThisFrame) CastSkill("ritual");
            }
            if (_skillBar != null) _skillBar.gameObject.SetActive(_modal == Modal.None && !_model.Frozen && _model.Mode == InputMode.Work && _model.Skills.States.Values.Any(state => state.Level > 0));
            foreach (var entry in _skillButtons)
            {
                var state = _model.Skills.States[entry.Key]; string key = entry.Key == "lightning" ? "Q" : entry.Key == "abundance" ? "W" : "E";
                var data = ProgressionCatalog.Skills.Find(skill => skill.Id == entry.Key);
                entry.Value.interactable = !_model.Settings.MvpSlots && state.Level > 0 && state.CooldownRemaining <= 0 && _model.HasAltar;
                entry.Value.GetComponentInChildren<TMP_Text>().text = (_model.Settings.MvpSlots ? "[자동] " : "[" + key + "] ") + data.Title + "\n" + (state.Level == 0 ? "미해금" : state.ActiveRemaining > 0 ? "발동 중 " + state.ActiveRemaining.ToString("0.0") : state.CooldownRemaining > 0 ? state.CooldownRemaining.ToString("0.0") + "초" : !_model.HasAltar ? "제단 필요" : "준비");
            }
            for (int index = _skillBolts.Count - 1; index >= 0; index--)
                if (Time.unscaledTime >= _skillBolts[index].Expires) { Destroy(_skillBolts[index].Line.gameObject); _skillBolts.RemoveAt(index); }
        }
        private void ClearSkillPresentation()
        {
            _skillAimValid = false;
            foreach (var bolt in _skillBolts) if (bolt.Line != null) Destroy(bolt.Line.gameObject);
            _skillBolts.Clear();
        }
        private void CastSkill(string id)
        {
            if (_model.Settings.MvpSlots || _modal != Modal.None || _model.Frozen || _model.Mode != InputMode.Work || !_skillAimValid && id != "abundance") return;
            if (!_model.TryCastSkill(id, _skillAim, _skillAimValid)) PlaySound(SoundCue.Error);
        }
        private void ShowSkillLightning(Point origin, Point target)
        {
            var line = Line("AltarLightning", new Color(.65f, .8f, 1), .065f); line.enabled = true; line.loop = false; line.positionCount = 6;
            for (int index = 0; index < 6; index++)
            {
                float t = index / 5f; var point = Vector3.Lerp(V(origin, 4), V(target, .4f), t);
                if (index > 0 && index < 5) point.x += index % 2 == 0 ? .25f : -.25f;
                line.SetPosition(index, point);
            }
            _skillBolts.Add((line, Time.unscaledTime + .22f));
        }
    }
}
