using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.Wind;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    /// <summary>Read-only After presentation for an external camera; no showcase UI, input or camera ownership.</summary>
    public sealed class Day01FinalStagePreview : MonoBehaviour
    {
        public Transform Stage;
        public Camera ViewCamera;
        public SceneFeatureProfile Profile;
        public ParticleSystem SmokePrefab;
        private Day01StageEffects _effects;
        private WindFieldController _wind;
        private ShowcaseTarget[] _targets;
        private bool _started;
        private void Start() { _started = true; Create(); }
        private void OnEnable() { if (_started && Application.isPlaying) Create(); }
        private void Create()
        {
            if (_effects != null || Stage == null || ViewCamera == null) return;
            var settings = Profile != null ? Profile.Settings.Copy() : SceneFeatureProfile.AfterSettings();
            _targets = Stage.GetComponentsInChildren<ShowcaseTarget>(true);
            foreach (var target in _targets)
            {
                target.ReleasePresentation();
                target.BiomeQuery = world => { var p = Stage.InverseTransformPoint(world); return ShowcaseBiomeMap.At(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.z)); };
                target.GroundHeightQuery = world => Stage.position.y;
                target.Initialize();
                target.SetFeatureFlags(false, false, settings.IsEnabled(SceneFeature.Snow), false);
            }
            _wind = new WindFieldController();
            _effects = new Day01StageEffects(ViewCamera, SmokePrefab, _wind.Sample, () => { });
            _effects.Create(Stage, 5, _targets, settings);
            // Newly generated foliage/shoreline inherits the isolated preview layer.
            // Bloom volumes deliberately keep layer 31, as in the original After stage.
            foreach (var child in Stage.GetComponentsInChildren<Transform>(true))
                if (child.gameObject.layer != 31) child.gameObject.layer = 30;
        }
        private void Update()
        {
            if (_effects == null) return;
            _wind.Tick(Time.unscaledTime); _effects.Tick(Time.unscaledDeltaTime);
        }
        private void LateUpdate() => _effects?.LateTick();
        private void OnDisable()
        {
            _effects?.Dispose(); _effects = null;
            _wind?.Dispose(); _wind = null;
            if (_targets != null) foreach (var target in _targets) if (target != null) target.ReleasePresentation();
            _targets = null;
        }
    }
}
