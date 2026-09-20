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
        private RectTransform _skillBar;
        private readonly Dictionary<string, Button> _skillButtons = new Dictionary<string, Button>();
        private Point _skillAim;
        private bool _skillAimValid;
        private readonly List<(LineRenderer Line, float Expires)> _skillBolts = new List<(LineRenderer, float)>();

        private void PrepareProgressionUi()
        {
            if (ProgressionCatalog == null || _model.Upgrades == null) return;
            // 기존 제단의 환생 기록 화면은 별도 모달 창으로 보존한다.
            var altar = _sceneUi.Bindings.Find(binding => binding.Id == "Modal.Altar")?.Rect;
            if (altar == null) return;
            var review = Instantiate(altar.gameObject, altar.parent).GetComponent<RectTransform>(); review.name = "RebirthReview"; review.gameObject.SetActive(false);
            var panel = review.GetComponent<PrototypeModalPanel>();
            if (panel != null)
            {
                foreach (var binding in panel.Bindings) binding.Id = binding.Id.Replace("Modal.Altar", "Modal.RebirthReview").Replace("Altar.", "RebirthReview.");
                panel.RetiredBindingIds.Clear();
            }
            else
            {
                panel = review.gameObject.AddComponent<PrototypeModalPanel>();
                foreach (var binding in _sceneUi.Bindings.Where(binding => binding.Rect != null && (binding.Rect == altar || binding.Rect.IsChildOf(altar))).ToArray())
                {
                    var path = RelativeTransformPath(altar, binding.Rect); var mapped = string.IsNullOrEmpty(path) ? review : review.Find(path) as RectTransform;
                    if (mapped == null) continue;
                    panel.Bindings.Add(new PrototypeUiBinding { Id = binding.Id.Replace("Modal.Altar", "Modal.RebirthReview").Replace("Altar.", "RebirthReview."), Rect = mapped,
                        Text = mapped.GetComponent<Text>(), TmpText = mapped.GetComponent<TMP_Text>(), Button = mapped.GetComponent<Button>(), Image = mapped.GetComponent<Image>() });
                }
            }
            panel.Connect(_sceneUi);
        }
        private static string RelativeTransformPath(Transform root, Transform child)
        {
            var parts = new List<string>(); while (child != root) { parts.Add(child.name); child = child.parent; } parts.Reverse(); return string.Join("/", parts);
        }
        private void InitializeProgressionUi()
        {
            if (ProgressionCatalog == null || _model.Upgrades == null) return;
            foreach (var tree in ProgressionCatalog.Trees)
            {
                var modal = tree.Id == "research" ? Modal.Research : Modal.Altar;
                var root = _sceneUi.Item("Modal." + modal)?.Rect; if (root == null) continue;
                foreach (Transform child in root) child.gameObject.SetActive(false);
                var surface = root.GetComponent<PrototypeModalSurface>(); if (surface == null) surface = root.gameObject.AddComponent<PrototypeModalSurface>();
                surface.Presentation = ModalPresentation.Screen;
                if (tree.Id == "altar")
                {
                    // 화면 뒤의 암전/블러는 Modal Surface 설정에 맡긴다.
                    var panelImage = root.GetComponent<Image>();
                    if (panelImage != null) panelImage.enabled = false;
                }
                UpgradeTreeScreen.Stretch(root);
                var screenRect = UpgradeTreeScreen.MakeRect("UpgradeTreeScreen", root); UpgradeTreeScreen.Stretch(screenRect);
                var screen = screenRect.gameObject.AddComponent<UpgradeTreeScreen>();
                string treeId = tree.Id;
                screen.Bind(tree, _model.Upgrades, _sceneUi.ResourcesTmpFont, _model.BuyProgression, () => _model.CanUseTree(treeId), () => _model.Money, CloseModal,
                    treeId == "altar" ? (Action)(() => PushModal(Modal.RebirthReview)) : null, () => !_model.Ended,
                    node => Resources.Load<Sprite>("UI/Upgrades/" + node.Id));
            }
            _skillBar = UpgradeTreeScreen.MakeRect("AltarSkillBar", _sceneUi.Canvas.transform); _skillBar.anchorMin = _skillBar.anchorMax = new Vector2(.5f, 0); _skillBar.pivot = new Vector2(.5f, 0); _skillBar.anchoredPosition = new Vector2(0, 16); _skillBar.sizeDelta = new Vector2(480, 48);
            string[] ids = { "lightning", "abundance", "ritual" };
            for (int index = 0; index < ids.Length; index++)
            {
                string id = ids[index]; var rect = UpgradeTreeScreen.MakeRect(id, _skillBar); rect.sizeDelta = new Vector2(150, 48); rect.anchoredPosition = new Vector2((index - 1) * 160, 0);
                var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.09f, .19f, .24f, .96f);
                var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.onClick.AddListener(() => CastSkill(id));
                button.navigation = new Navigation { mode = Navigation.Mode.None };
                var outline = rect.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(.75f, .57f, .27f); outline.effectDistance = new Vector2(1, -1);
                var labelRect = UpgradeTreeScreen.MakeRect("Label", rect); UpgradeTreeScreen.Stretch(labelRect);
                var label = labelRect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = _sceneUi.ResourcesTmpFont; label.fontSize = 17; label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
                _skillButtons.Add(id, button);
            }
            _model.LightningStruck += ShowSkillLightning;
        }
        private void UpdateProgressionInput()
        {
            if (_model.Skills == null) return;
            if (_pointerValid && _model.PointerVisible && _modal == Modal.None) { _skillAim = _pointer; _skillAimValid = true; }
            if (_modal == Modal.None && !_model.Frozen && !_tileBuildMenu && Application.isFocused && Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame) CastSkill("lightning");
                if (Keyboard.current.wKey.wasPressedThisFrame) CastSkill("abundance");
                if (Keyboard.current.eKey.wasPressedThisFrame) CastSkill("ritual");
            }
            if (_skillBar != null) _skillBar.gameObject.SetActive(_modal == Modal.None && !_model.Frozen && _model.Skills.States.Values.Any(state => state.Level > 0));
            foreach (var entry in _skillButtons)
            {
                var state = _model.Skills.States[entry.Key]; string key = entry.Key == "lightning" ? "Q" : entry.Key == "abundance" ? "W" : "E";
                var data = ProgressionCatalog.Skills.Find(skill => skill.Id == entry.Key);
                entry.Value.interactable = state.Level > 0 && state.CooldownRemaining <= 0 && _model.Has(Kind.Altar);
                entry.Value.GetComponentInChildren<TMP_Text>().text = "[" + key + "] " + data.Title + "\n" + (state.Level == 0 ? "미해금" : state.ActiveRemaining > 0 ? "발동 중 " + state.ActiveRemaining.ToString("0.0") : state.CooldownRemaining > 0 ? state.CooldownRemaining.ToString("0.0") + "초" : "준비");
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
            if (_modal != Modal.None || _model.Frozen || !_skillAimValid && id != "abundance") return;
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
