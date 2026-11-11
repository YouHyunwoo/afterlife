using Afterlife.Modules.Destruction;
using Afterlife.Modules.Hover;
using Afterlife.Devlog.Shoreline;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // IMGUI presentation for the filmed six-stage comparison.
    internal sealed class Day01GuidePresenter
    {
        private const float GuideScale = 1.5f;
        private static readonly string[] PhaseNames =
        { "BEFORE / PRIMITIVES", "SHAPE / PIXEL STYLE", "TERRAIN / WATER", "LIVING ENVIRONMENT", "INTERACTION", "AFTER / ATMOSPHERE" };
        private static readonly string[] StageButtons = { "1 Before", "2 Shape", "3 Terrain", "4 Life", "5 Hits", "6 After" };
        private static readonly string[] ShoreNames = { "Square", "Bevel", "Rounded" };
        private readonly Day01Showcase _showcase;

        public Day01GuidePresenter(Day01Showcase showcase) { _showcase = showcase; }

        public static Rect GuideRect => new Rect(16, 16, 550 * GuideScale, 342 * GuideScale);

        public void Draw(Day01PixelOutput output, Day01StageEffects effects)
        {
            if (_showcase.ShowcaseCamera == null) return;
            GUI.depth = 100;
            output?.DrawLetterbox();
            GUI.color = Color.white;
            if (!_showcase.ShowGuide || _showcase.FilmMode) return;
            var previousMatrix = GUI.matrix;
            try
            {
                GUI.matrix = previousMatrix * Matrix4x4.TRS(new Vector3(16 * (1 - GuideScale), 16 * (1 - GuideScale), 0),
                    Quaternion.identity, new Vector3(GuideScale, GuideScale, 1));
                DrawControls(effects);
            }
            finally { GUI.matrix = previousMatrix; }
        }

        private void DrawControls(Day01StageEffects effects)
        {
            int phase = _showcase.CurrentStageIndex;
            GUI.Box(new Rect(16,16,550,342), "");
            GUI.Label(new Rect(28, 23, 520, 22), "DAY 01   /   " + PhaseNames[phase]);
            GUI.Label(new Rect(28, 48, 520, 22), "1~6 Stages | H Hide guide | F8 Film mode");
            GUI.Label(new Rect(28, 73, 520, 22), "P Pixels   R Reset   G Leaf rotation: " +
                (effects != null && effects.LeafRotation ? "Random 360" : "Uniform"));
            GUI.Label(new Rect(28, 98, 520, 22), (phase == 5 ? "N Day/Night" : "Day/Night: stage 6") +
                " | L Leaves: " + (effects != null && effects.LeavesEnabled ? "ON " : "OFF ") +
                (effects?.TreeCount ?? 0) + " trees / " + (effects?.LeafCount ?? 0) + " cards");

            GUI.enabled = phase >= 4;
            if (GUI.Button(new Rect(28,126,180,27),
                (_showcase.RemovalMode == DisappearMode.Sink ? "[Selected] " : "") + "Sink + reset"))
                _showcase.SelectRemoval(DisappearMode.Sink);
            if (GUI.Button(new Rect(218,126,200,27),
                (_showcase.RemovalMode == DisappearMode.Dissolve ? "[Selected] " : "") + "Dissolve + reset"))
                _showcase.SelectRemoval(DisappearMode.Dissolve);

            GUI.enabled = phase >= 2;
            for (int i = 0; i < 3; i++)
            {
                var mode = (ShorelineMode)i;
                if (GUI.Button(new Rect(28 + i * 170,161,160,27),
                    (effects?.CurrentShorelineMode == mode ? "* " : "") + "Coast: " + ShoreNames[i]))
                    _showcase.SelectShoreline(mode);
            }

            GUI.enabled = phase >= 4;
            if (GUI.Button(new Rect(28,196,245,27),
                (effects?.GrassMode == GrassHoverMode.Radial ? "* " : "") + "Grass: Radial bend"))
                effects?.SetGrassMode(GrassHoverMode.Radial);
            if (GUI.Button(new Rect(283,196,245,27),
                (effects?.GrassMode == GrassHoverMode.Sway ? "* " : "") + "Grass: Sway + recover"))
                effects?.SetGrassMode(GrassHoverMode.Sway);
            if (GUI.Button(new Rect(28,233,500,27), "Damage marks: " +
                (_showcase.DamageMarksEnabled ? "ON" : "OFF") + "  / compare same hits"))
                _showcase.ToggleDamageMarks();

            GUI.enabled = phase == 5;
            _showcase.VisionTest = GUI.Toggle(new Rect(28,307,500,27), _showcase.VisionTest,
                "V Vision blur: " + (_showcase.VisionTest ? "ON" : "OFF"));
            GUI.enabled = true;
            for (int i = 0; i < StageButtons.Length; i++)
                if (GUI.Button(new Rect(28 + i * 86, 270, 82, 27),
                    (phase == i ? "* " : "") + StageButtons[i])) _showcase.SetStage(i);
        }
    }
}
