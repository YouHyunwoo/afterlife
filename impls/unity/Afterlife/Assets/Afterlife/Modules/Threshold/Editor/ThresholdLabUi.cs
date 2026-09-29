using Afterlife.Modules.Threshold.Unity;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Afterlife.Modules.Threshold.Editor
{
    internal static class ThresholdLabUi
    {
        private static readonly Color White = new Color(.87f, .91f, .93f);
        private static readonly Color Muted = new Color(.48f, .58f, .63f);
        private static readonly Color Gold = new Color(.86f, .72f, .46f);
        private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_AfterlifePrototype/Resources/Fonts/Mona12TextKR SDF.asset");
        private static TMP_FontAsset LogoFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/05_Resources/Externals/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");

        internal static GameObject Page(ThresholdScreen screen)
        {
            var root = Rect(screen + "Page", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var group = root.AddComponent<CanvasGroup>();
            var data = root.AddComponent<ThresholdPage>(); data.Screen = screen; data.Group = group;
            switch (screen)
            {
                case ThresholdScreen.Title:
                    Text(root.transform, "TitleKicker", "THE HERO'S TRIAL", 240, 260, 650, 30, 18, Muted, true);
                    Text(root.transform, "Title", "AFTERLIFE", 230, 308, 960, 120, 88, White, true);
                    Text(root.transform, "Subtitle", "잠든 육신 너머, 영웅의 시험이 시작된다.", 244, 449, 670, 46, 23, Muted);
                    Rule(root.transform, 245, 529, 66, Gold);
                    Button(root.transform, "NewGame", "새 게임", 238, 573, 350, ThresholdAction.NewGame);
                    Button(root.transform, "Settings", "설정", 238, 648, 350, ThresholdAction.OpenModal);
                    Button(root.transform, "Quit", "종료", 238, 723, 350, ThresholdAction.Quit);
                    Footer(root.transform, "SCREEN FLOW STUDY     /     01", "공간과 흐름을 확인하는 테스트 씬");
                    break;
                case ThresholdScreen.Lobby:
                    ConfigureIntro(root);
                    break;
                case ThresholdScreen.Playing:
                    Text(root.transform, "PlayingKicker", "THE WAKING WORLD", 110, 96, 650, 32, 18, Muted, true);
                    Text(root.transform, "PlayingTitle", "시험의 땅", 105, 145, 720, 86, 48, White);
                    Text(root.transform, "PlayingNote", "빈 인게임 화면 · 건설과 전투는 연결하지 않았습니다.", 110, 249, 1120, 50, 23, Muted);
                    Button(root.transform, "OpenModal", "설정 / 모달 보기", 110, 707, 410, ThresholdAction.OpenModal);
                    Button(root.transform, "DemoFail", "테스트: 게임 오버", 110, 787, 410, ThresholdAction.Fail);
                    Button(root.transform, "DemoClear", "테스트: 게임 클리어", 570, 787, 440, ThresholdAction.Complete);
                    Button(root.transform, "BackLobby", "로비로", 110, 867, 410, ThresholdAction.Lobby);
                    Footer(root.transform, "FLOW TEST ONLY", "ESC  설정 열기 / 닫기");
                    break;
                case ThresholdScreen.GameOver:
                    Text(root.transform, "FailureKicker", "THE TRIAL FADES", 240, 220, 900, 35, 18, Muted, true);
                    Text(root.transform, "FailureTitle", "시험은 끝나지 않았다", 230, 292, 1200, 100, 55, White);
                    Rule(root.transform, 242, 442, 80, new Color(.6f, .39f, .35f));
                    Text(root.transform, "FailureStory", "마을의 불빛이 어둠에 잠겼다.\n다시 한 번, 돌아갈 길을 찾아라.", 242, 494, 960, 100, 26, Muted);
                    Button(root.transform, "Retry", "다시 도전", 232, 677, 420, ThresholdAction.EnterTrial);
                    Button(root.transform, "Lobby", "로비로", 232, 760, 420, ThresholdAction.Lobby);
                    Button(root.transform, "Title", "타이틀로", 232, 843, 420, ThresholdAction.Title);
                    Footer(root.transform, "GAME OVER", "결과 수치 없이 화면 흐름만 확인합니다.");
                    break;
                case ThresholdScreen.Clear:
                    Text(root.transform, "ClearKicker", "THE LIGHT REMAINS", 150, 220, 620, 35, 18, Gold, true);
                    Text(root.transform, "ClearTitle", "돌아갈 자격", 145, 292, 690, 96, 57, White);
                    Rule(root.transform, 152, 436, 80, Gold);
                    Text(root.transform, "ClearStory", "마을은 밤을 이겨냈다.\n이제, 잠든 세계로 돌아갈 시간이다.", 150, 485, 580, 110, 25, White);
                    Text(root.transform, "ClearNote", "영웅의 시험을 통과했습니다.", 150, 628, 580, 46, 22, Gold);
                    Button(root.transform, "ReturnTitle", "타이틀로 돌아가기", 142, 741, 500, ThresholdAction.Title);
                    Button(root.transform, "Lobby", "로비 다시 보기", 142, 824, 500, ThresholdAction.Lobby);
                    Footer(root.transform, "TRIAL COMPLETE", "귀환 이후의 모험은 이 테스트 씬에 포함되지 않습니다.");
                    break;
            }
            return root;
        }

        internal static void ConfigureIntro(GameObject root)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
            Text(root.transform, "Speaker", "신의 대리자", 250, 792, 1400, 40, 21, Gold);
            Text(root.transform, "Narration", "", 250, 850, 1420, 105, 30, White);
            Text(root.transform, "AdvanceHint", "클릭 / Enter  다음     Esc  건너뛰기", 1190, 980, 620, 40, 21, White);
            root.transform.Find("AdvanceHint").GetComponent<TMP_Text>().alignment = TextAlignmentOptions.Right;
            var progress = Rect("DialogueProgress", root.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(250, -995), new Vector2(600, 24));
            progress.pivot = new Vector2(0, .5f);
            var intro = root.GetComponent<ThresholdIntro>();
            if (intro == null) intro = root.AddComponent<ThresholdIntro>();
            intro.Subtitle = root.transform.Find("Narration").GetComponent<TMP_Text>();
            intro.Voice = root.GetComponent<AudioSource>();
            if (intro.Voice == null) intro.Voice = root.AddComponent<AudioSource>();
            intro.Voice.playOnAwake = false; intro.Voice.spatialBlend = 0;
            intro.Lines = new[] {
                "눈을 떴구나. 두려워 말거라.\n네 몸은 아직 저편에서 잠들어 있다.",
                "나는 신의 뜻을 전하는 자.\n너는 태어나던 날, 영웅의 시험을 치를 후보로 뽑혔다.",
                "저 틈 너머는 네가 살던 세계다.\n저 마을은 밤마다 마물의 습격을 받고 있지.",
                "네게 잠시 영력을 빌려주겠다.\n마을을 일으키고, 다가올 밤들을 견디게 하거라.",
                "시험을 마치면, 돌아갈 길이 열릴 것이다." };
            intro.ProgressRoot = progress;
        }

        internal static GameObject Modal()
        {
            var root = Rect("ModalPage", null, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).gameObject;
            var page = root.AddComponent<ThresholdPage>(); page.IsModal = true; page.Group = root.GetComponent<CanvasGroup>();
            // Invisible input shield blocks clicks to every page and the world while the modal is open.
            var shield = root.AddComponent<Image>(); shield.color = Color.clear; shield.raycastTarget = true;
            var panel = Rect("ModalSurface", root.transform, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(760, 650));
            panel.pivot = new Vector2(.5f, .5f);
            var surface = panel.gameObject.AddComponent<Image>(); surface.color = new Color(.018f, .027f, .035f, .97f);
            Text(panel, "ModalKicker", "A MOMENT BETWEEN", 70, 58, 650, 35, 17, Muted, true);
            Text(panel, "ModalTitle", "잠시, 경계에 머물다", 65, 118, 650, 65, 38, White);
            Rule(panel, 72, 218, 70, Gold);
            Text(panel, "VolumeLabel", "공간의 소리", 70, 259, 450, 45, 24, White);
            var sliderRoot = Rect("AmbienceVolume", panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -324), new Vector2(610, 28)); sliderRoot.pivot = new Vector2(0, 1);
            var track = Rect("Track", sliderRoot, new Vector2(0, .4f), new Vector2(1, .6f), Vector2.zero, Vector2.zero); track.gameObject.AddComponent<Image>().color = new Color(.11f, .16f, .2f);
            var handle = Rect("Handle", sliderRoot, new Vector2(0, 0), new Vector2(0, 1), Vector2.zero, new Vector2(16, 0)); var handleImage = handle.gameObject.AddComponent<Image>(); handleImage.color = Gold;
            var slider = sliderRoot.gameObject.AddComponent<Slider>(); slider.handleRect = handle; slider.targetGraphic = handleImage; slider.minValue = 0; slider.maxValue = .5f; slider.value = .13f;
            var toggleRoot = Rect("ReducedMotion", panel, new Vector2(0, 1), new Vector2(0, 1), new Vector2(70, -394), new Vector2(610, 44)); toggleRoot.pivot = new Vector2(0, 1);
            var box = Rect("CheckBox", toggleRoot, new Vector2(0, .5f), new Vector2(0, .5f), new Vector2(14, 0), new Vector2(28, 28)); var boxImage = box.gameObject.AddComponent<Image>(); boxImage.color = new Color(.12f, .17f, .21f);
            var check = Rect("Check", box, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(14, 14)); var checkImage = check.gameObject.AddComponent<Image>(); checkImage.color = Gold;
            var toggle = toggleRoot.gameObject.AddComponent<Toggle>(); toggle.targetGraphic = boxImage; toggle.graphic = checkImage;
            Text(toggleRoot, "ReducedMotionLabel", "전환 시간과 일렁임 줄이기", 50, 2, 520, 44, 23, White);
            var options = root.AddComponent<ThresholdOptions>(); options.Volume = slider; options.ReducedMotion = toggle;
            Button(panel, "Close", "계속하기", 62, 505, 610, ThresholdAction.CloseModal);
            Text(panel, "ModalFooter", "ESC  닫기 · 설정은 이 실행 동안만 유지됩니다.", 70, 593, 630, 32, 17, Muted);
            return root;
        }
        internal static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 position, Vector2 size)
        {
            var obj = new GameObject(name, typeof(RectTransform)); var rect = obj.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static void Text(Transform parent, string name, string text, float x, float y, float width, float height, float size, Color color, bool latin = false)
        {
            var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, height)); rect.pivot = new Vector2(0, 1);
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>(); label.font = latin ? LogoFont : Font;
            if (label.font == null) throw new System.InvalidOperationException("Required TMP font missing: " + name);
            label.text = text; label.fontSize = size; label.color = color; label.raycastTarget = false;
            label.textWrappingMode = TextWrappingModes.Normal; label.overflowMode = TextOverflowModes.Overflow;
            label.alignment = TextAlignmentOptions.Left; label.characterSpacing = latin ? 5 : 0;
        }
        private static void Button(Transform parent, string name, string label, float x, float y, float width, ThresholdAction action)
        {
            var rect = Rect(name, parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, 64)); rect.pivot = new Vector2(0, 1);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = new Color(.025f, .035f, .045f, .25f); colors.highlightedColor = new Color(.12f, .19f, .23f, .9f); colors.selectedColor = colors.highlightedColor; colors.pressedColor = new Color(.22f, .3f, .32f, 1); colors.fadeDuration = .18f; button.colors = colors;
            var binding = rect.gameObject.AddComponent<ThresholdActionButton>(); binding.Action = action;
            Text(rect, "ActionLabel", label, 18, 7, width - 36, 50, 26, White);
        }
        private static void Rule(Transform parent, float x, float y, float width, Color color)
        {
            var rect = Rect("AccentLine", parent, new Vector2(0, 1), new Vector2(0, 1), new Vector2(x, -y), new Vector2(width, 2)); rect.pivot = new Vector2(0, 1); rect.gameObject.AddComponent<Image>().color = color;
        }
        private static void Footer(Transform parent, string left, string right)
        {
            Text(parent, "FooterLeft", left, 80, 1017, 760, 32, 15, Muted, true);
            Text(parent, "FooterRight", right, 1050, 1017, 790, 34, 17, Muted);
        }
    }
}
