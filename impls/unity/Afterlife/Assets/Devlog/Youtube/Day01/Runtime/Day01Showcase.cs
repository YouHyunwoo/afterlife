using Afterlife.Modules.Interaction;
using Afterlife.Modules.SceneFeatures;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

namespace Afterlife.Devlog.Day01
{
    public sealed class Day01Showcase : MonoBehaviour
    {
        public SceneFeatureProfile FeatureProfile;
        public SceneFeatureOverrides FeatureOverrides=new SceneFeatureOverrides();
        private SceneFeatureSettings _featureSettings;
        internal bool Feature(SceneFeature feature)=>_featureSettings==null||_featureSettings.IsEnabled(feature);
        public Camera ShowcaseCamera;
        public bool FilmMode { get; set; }
        public bool FilmInteraction { get; set; }
        private bool _filmVision;
        public bool FilmVision
        {
            get => _filmVision;
            set { if (_filmVision == value) return; _filmVision = value; if (_output != null && _stages != null) ConfigureOutput(); }
        }
        public void SetFilmLightingBlend(float? blend) => _lighting?.SetFilmBlend(blend);
        public int CurrentStageIndex => _stages?.Index ?? StartStage;
        public Vector3 StageOrigin => CurrentStage.position;
        public void SetFilmNight(bool night, bool immediate) => _lighting?.SetNight(night, immediate);
        public void SetFilmLook()
        {
            if (_effects?.SelectShoreline(Afterlife.Devlog.Shoreline.ShorelineMode.Rounded, false) == true && _pickSurfaces != null)
                _pickIndex.Rebuild(_pickSurfaces);
            _effects?.SetGrassMode(Afterlife.Modules.Hover.GrassHoverMode.Sway, false);
            _removalMode=Afterlife.Modules.Destruction.DisappearMode.Dissolve;
            _damageMarksEnabled=true;
            foreach(var target in _targets) { target.RemovalMode=_removalMode; target.DamageMarksEnabled=true; }
        }
        private Day01PixelOutput _output;
        private Day01GuidePresenter _guide;
        private Day01StageController _stages;
        private Day01PointerInput _pointer;
        public Transform[] PresentationStages;
        public Transform[] StageRoots => PresentationStages;
        private Transform CurrentStage => _stages.Current;
        private int Phase => _stages.Index;
        public UniversalRenderPipelineAsset Pipeline;
        public Material ParticleMaterial;
        public GameObject Axe, Pick;
        [Range(0, 5)] public int StartStage = 5;
        [Range(180, 720)] public int PixelHeight = 360;
        public bool ShowGuide = true;
        public bool CameraShake = true;
        [Header("Day / night vision")]
        [Min(.1f)] public float VisionHeight = .8f;
        [Min(1)] public float CursorVisionRadius = 3.2f;
        [Min(1)] public float HouseVisionRadius = 3f;
        [Min(.1f)] public float VisionTrailSeconds=1.4f;
        private ShowcaseLighting _lighting;
        private bool _visionTest;
        private bool VisionEnabled => Feature(SceneFeature.Vision) && Phase == 5 && (FilmMode ? FilmVision : _visionTest);
        private Afterlife.Modules.Wind.WindFieldController _wind;
        private Day01StageEffects _effects;
        private ShowcaseTarget[] _targets;
        private PickSurface[] _pickSurfaces;
        private readonly PickSurfaceIndex _pickIndex=new PickSurfaceIndex();
        private Day01ImpactPresentation _impact;
        public Afterlife.Modules.WorldAudio.WorldAudioBank AudioBank;
        public Afterlife.Modules.AmbientAudio.AmbientAudioSettings Ambience = new Afterlife.Modules.AmbientAudio.AmbientAudioSettings();
        private Afterlife.Modules.AmbientAudio.AmbientAudioController _ambience;
        public Afterlife.Modules.Destruction.DestructionProfile DestructionSettings;
        public GameObject ChipPrefab;
        public ParticleSystem SmokePrefab;
        private Afterlife.Modules.WorldAudio.WorldAudioPlayer _sound;
        private bool _pixels = true;
        private readonly Vector3 _cameraOffset = new Vector3(10, 13, -16);

        private static SceneFeatureSettings LegacyFeatures() { var s=SceneFeatureProfile.AfterSettings();s.Set(SceneFeature.Vision,true);s.Set(SceneFeature.VisionBlur,true);return s; }
        private void OnEnable()
        {
            if (ShowcaseCamera == null || Pipeline == null || AudioBank == null || PresentationStages == null || PresentationStages.Length != 6)
            { Debug.LogError("Day01: camera, pipeline, audio bank or six stages missing.", this); enabled = false; return; }
            foreach (var stage in PresentationStages)
                if (stage == null) { Debug.LogError("Day01: presentation stage missing.", this); enabled = false; return; }
            _featureSettings=FeatureOverrides.Resolve(FeatureProfile!=null?FeatureProfile.Settings:LegacyFeatures());
            _stages = new Day01StageController(PresentationStages);
            _output = new Day01PixelOutput(transform, ShowcaseCamera, Pipeline);
            _guide = new Day01GuidePresenter(this);
            var targets = new List<ShowcaseTarget>();
            foreach (var stage in StageRoots) targets.AddRange(stage.GetComponentsInChildren<ShowcaseTarget>(true));
            _targets = targets.ToArray();
            foreach (var target in _targets) { target.Initialize(); target.DamageMarksEnabled=_damageMarksEnabled; }
            var bank=AudioBank;
            _sound=new Afterlife.Modules.WorldAudio.WorldAudioPlayer(transform,bank,
                bank.WaterRipple!=null ? bank.WaterRipple : Resources.Load<AudioClip>("WorldAudio/freesori_water_ripple"),
                Resources.LoadAll<AudioClip>("WorldAudio/WaterRipples"));
            _ambience = new Afterlife.Modules.AmbientAudio.AmbientAudioController(transform, Ambience,
                new[] {
                    Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_02"),
                    Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_03"),
                    Resources.Load<AudioClip>("AmbientSounds/GrassRustles/grass_rustle_04")
                },
                Resources.Load<AudioClip>("AmbientSounds/bird"));
            foreach (var target in _targets) if (DestructionSettings!=null) target.DestructionSettings=DestructionSettings;
            _lighting = new ShowcaseLighting(transform,ShowcaseCamera,new Rect(-6,-5,12,10))
            { Height = _featureSettings.VisionHeight, CursorRadius = _featureSettings.CursorRadius, HouseRadius = _featureSettings.SourceRadius, TrailSeconds = _featureSettings.TrailSeconds };
            _wind = new Afterlife.Modules.Wind.WindFieldController();
            _impact = new Day01ImpactPresentation(transform, ShowcaseCamera, Axe, Pick, ChipPrefab, ParticleMaterial, _sound);
            _effects = new Day01StageEffects(ShowcaseCamera, SmokePrefab, _wind.Sample, PlayWaterRipple);
            _pointer = new Day01PointerInput(ShowcaseCamera, _output, _lighting, _effects, _impact, _pickIndex);
            _impact.ToolsEnabled=Feature(SceneFeature.Tools);_impact.ClickParticlesEnabled=Feature(SceneFeature.ClickParticles);_impact.HitParticlesEnabled=Feature(SceneFeature.HitFeedback);_impact.SoundEnabled=Feature(SceneFeature.Sound);
            _impact.ImpactTime=_featureSettings.ToolContactSeconds;_impact.SwingDuration=_featureSettings.ToolSwingSeconds;
            _lighting.DayNightEnabled=Feature(SceneFeature.DayNight);_lighting.NightSky=_featureSettings.NightSky;_lighting.NightSun=_featureSettings.NightSun;_lighting.NightIntensity=_featureSettings.NightIntensity;_lighting.TransitionSpeed=_featureSettings.DayNightSpeed;
            foreach(var target in _targets) { target.HitFeedbackEnabled=Feature(SceneFeature.HitFeedback);target.DestructionEnabled=Feature(SceneFeature.Destruction);target.SetFeatureFlags(Feature(SceneFeature.HitFeedback),Feature(SceneFeature.DamageMarks),Feature(SceneFeature.Snow),Feature(SceneFeature.Destruction)); }
            SetStage(StartStage);
        }

        private void PlayWaterRipple() { if(Feature(SceneFeature.Sound))_sound?.PlayWaterRipple(Time.unscaledTime); }

        public void SetStage(int stage)
        {
            ResetTake();
            DisposeStageEffects();
            _stages.Select(stage);
            if (Phase != 5) _visionTest = false;
            _lighting?.SetStage(CurrentStage);
            _lighting?.SetNight(_featureSettings.Night, true);
            if (Phase == 5)
            {
                _pixels = true;
                _damageMarksEnabled = true;
                _removalMode = Afterlife.Modules.Destruction.DisappearMode.Dissolve;
                foreach (var target in _targets)
                {
                    target.DamageMarksEnabled = Feature(SceneFeature.DamageMarks);
                    target.RemovalMode = _removalMode;
                }
            }
            CreateStageEffects();
            _pickSurfaces = CurrentStage.GetComponentsInChildren<PickSurface>(true);
            _pickIndex.Rebuild(_pickSurfaces);
            ConfigureOutput();
            PositionCamera();
        }

        private void CreateStageEffects() => _effects?.Create(CurrentStage, Phase, _targets, _featureSettings);

        private void DisposeStageEffects()
        {
            _pickIndex.Clear(); _pickSurfaces = null;
            _effects?.Dispose();
        }

        private void ConfigureOutput() => _output?.Configure(_pixels && Phase >= 1 && Feature(SceneFeature.Pixels), VisionEnabled && Feature(SceneFeature.VisionBlur), PixelHeight);

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && !FilmMode)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SetStage(0);
                if (keyboard.digit2Key.wasPressedThisFrame) SetStage(1);
                if (keyboard.digit3Key.wasPressedThisFrame) SetStage(2);
                if (keyboard.digit4Key.wasPressedThisFrame) SetStage(3);
                if (keyboard.digit5Key.wasPressedThisFrame) SetStage(4);
                if (keyboard.digit6Key.wasPressedThisFrame) SetStage(5);
                if (keyboard.hKey.wasPressedThisFrame) ShowGuide = !ShowGuide;
                if (keyboard.pKey.wasPressedThisFrame) { _pixels = !_pixels; ConfigureOutput(); }
                if (keyboard.rKey.wasPressedThisFrame) ResetTake();
                if (keyboard.vKey.wasPressedThisFrame && Phase == 5) VisionTest=!VisionTest;
                if (keyboard.nKey.wasPressedThisFrame && Phase == 5) _lighting?.Toggle();
                if (keyboard.lKey.wasPressedThisFrame) _effects?.ToggleLeaves();
                if (keyboard.gKey.wasPressedThisFrame) _effects?.ToggleLeafRotation();
            }
            _lighting?.SetVisionTest(VisionEnabled);
            _lighting?.Tick(Time.unscaledDeltaTime);
            _wind?.Tick(Time.unscaledTime, Phase >= 3 && Feature(SceneFeature.Wind) ? _featureSettings.WindStrength : 0);
            _effects?.Tick(Time.unscaledDeltaTime);
            _output?.ResizeIfNeeded(_pixels && Phase >= 1 && Feature(SceneFeature.Pixels), VisionEnabled && Feature(SceneFeature.VisionBlur), PixelHeight);
            _impact?.Tick(Time.unscaledDeltaTime);
            PositionCamera();
            if(Feature(SceneFeature.Picking)) _pointer?.Tick(CurrentStage, Phase, _visionTest, FilmMode, FilmInteraction, ShowGuide);
            _output?.SetVisionBlur(VisionEnabled && Feature(SceneFeature.VisionBlur), _lighting);
            _ambience?.Tick(Time.unscaledDeltaTime, Phase >= 3 && Feature(SceneFeature.Ambience), _lighting == null || !_lighting.Night,
                _pointer != null && _pointer.BrushingGrass);
        }

        // 자원 흔들림의 Update 이후 실제 표면 위치와 날 위치를 맞춘다.
        private void LateUpdate()
        {
            _impact?.AnimateTool(Time.unscaledDeltaTime);
            _effects?.LateTick();
        }

        private void PositionCamera()
        {
            if (FilmMode) return;
            Vector3 focus = CurrentStage.position + Vector3.up * .5f;
            ShowcaseCamera.transform.SetPositionAndRotation(focus + _cameraOffset, Quaternion.LookRotation(-_cameraOffset));
            float shake = _impact?.Shake ?? 0;
            if (CameraShake && shake > 0) ShowcaseCamera.transform.position += ShowcaseCamera.transform.right * Mathf.Sin(shake * 160) * shake * .2f;
        }

        public void ResetTake()
        {
            ResetTake(true);
        }

        private void ResetTake(bool resetAmbience)
        {
            _lighting?.ResetVisionTrail();
            _sound?.Reset();
            if (resetAmbience) _ambience?.Reset();
            _effects?.ResetTake();
            if (_targets != null) foreach (var target in _targets) target.ResetTake();
            _impact?.Reset();
            _pointer?.Reset();
        }

        private Afterlife.Modules.Destruction.DisappearMode _removalMode;
        internal void SelectRemoval(Afterlife.Modules.Destruction.DisappearMode mode)
        {
            _removalMode=mode;
            ResetTake();
            foreach (var target in _targets) target.RemovalMode=mode;
        }
        private bool _damageMarksEnabled = true;
        internal Afterlife.Modules.Destruction.DisappearMode RemovalMode => _removalMode;
        internal bool DamageMarksEnabled => _damageMarksEnabled;
        internal bool VisionTest
        {
            get => _visionTest;
            set { if (_visionTest == value) return; _visionTest = value; if (_output != null && _stages != null) ConfigureOutput(); }
        }

        internal void SelectShoreline(Afterlife.Devlog.Shoreline.ShorelineMode mode)
        {
            if (_effects?.SelectShoreline(mode) == true && _pickSurfaces != null) _pickIndex.Rebuild(_pickSurfaces);
        }

        internal void ToggleDamageMarks()
        {
            _damageMarksEnabled = !_damageMarksEnabled;
            if (_targets != null) foreach (var target in _targets) target.DamageMarksEnabled = _damageMarksEnabled;
        }

        private void OnGUI() => _guide?.Draw(_output, _effects);

        private void OnDisable()
        {
            ResetTake();
            _sound?.Dispose(); _sound=null;
            _ambience?.Dispose(); _ambience=null;
            DisposeStageEffects();
            _effects = null;
            _wind?.Dispose(); _wind = null;
            _lighting?.Dispose(); _lighting = null;
            _impact?.Dispose(); _impact=null;
            _output?.Dispose(); _output = null;
            _pointer = null;
            _guide = null;
            _stages = null;
        }
    }
}
