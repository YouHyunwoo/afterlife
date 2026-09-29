using Afterlife.Modules.Interaction;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Devlog.Day01
{
    // Resolves the visible pixel to one pick surface and sends interaction feedback.
    internal sealed class Day01PointerInput
    {
        private readonly Camera _camera;
        private readonly Day01PixelOutput _output;
        private readonly ShowcaseLighting _lighting;
        private readonly Day01StageEffects _effects;
        private readonly Day01ImpactPresentation _impact;
        private readonly PickSurfaceIndex _pickIndex;
        private ShowcaseTarget _hover;

        public bool BrushingGrass { get; private set; }

        public Day01PointerInput(Camera camera, Day01PixelOutput output, ShowcaseLighting lighting,
            Day01StageEffects effects, Day01ImpactPresentation impact, PickSurfaceIndex pickIndex)
        {
            _camera = camera;
            _output = output;
            _lighting = lighting;
            _effects = effects;
            _impact = impact;
            _pickIndex = pickIndex;
        }

        public void Reset()
        {
            if (_hover != null) _hover.SetHover(false);
            _hover = null;
            BrushingGrass = false;
            _effects?.ClearPointer();
            _lighting?.UpdateCursor(null);
        }

        public void Tick(Transform stage, int phase, bool visionTest, bool filmMode, bool filmInteraction, bool showGuide)
        {
            Reset();
            if ((phase < 4 && !visionTest) || (filmMode && !filmInteraction)) return;
            var mouse = Mouse.current;
            if (mouse == null || !Application.isFocused) return;
            Vector2 point = mouse.position.ReadValue();
            Vector2 guiPoint = new Vector2(point.x, Screen.height - point.y);
            if (showGuide && Day01GuidePresenter.GuideRect.Contains(guiPoint)) return;
            Rect display = _output.Display;
            if (!display.Contains(guiPoint)) return;
            var texture = _output.Texture;
            Vector3 viewport = new Vector3((point.x - display.x) / display.width, (point.y - display.y) / display.height, 0);
            if (texture != null)
            {
                viewport.x = (Mathf.Floor(viewport.x * texture.width) + .5f) / texture.width;
                viewport.y = (Mathf.Floor(viewport.y * texture.height) + .5f) / texture.height;
            }
            Ray ray = _camera.ViewportPointToRay(viewport);
            _lighting?.UpdateCursor(ray);
            if (phase < 4) return;
            if (!_pickIndex.Pick(ray, _camera.farClipPlane, out var surface, out var hitPoint, out var surfaceColor)) return;
            if (surface.Target == null && surface.HitNormal.y > .35f
                && (surface.name.StartsWith("Tile_") || surface.name == "OceanBlocks_HalfHeight"))
            {
                Vector3 local = stage.InverseTransformPoint(hitPoint);
                var biome = ShowcaseBiomeMap.At(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.z));
                bool moving = mouse.delta.ReadValue().sqrMagnitude > .01f;
                _effects?.SetPointer(hitPoint, biome, moving);
                BrushingGrass = moving && _effects != null && _effects.HasGrassAt(hitPoint);
            }
            // The nearest building or ruin surface hides resources behind it.
            _hover = surface.Target as ShowcaseTarget;
            if (_hover == null || !_hover.isActiveAndEnabled)
            {
                if (mouse.leftButton.wasPressedThisFrame && _impact != null && !_impact.IsSwinging)
                    _impact.TapFeedback(hitPoint, surface.HitNormal, surfaceColor);
                return;
            }
            _hover.SetHover(true);
            if (mouse.leftButton.wasPressedThisFrame) _impact?.BeginImpact(_hover, surface, hitPoint, surfaceColor);
        }
    }
}
