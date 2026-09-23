using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Rendering.Universal;

namespace Afterlife.Devlog.Day01
{
    public sealed class Day01Showcase : MonoBehaviour
    {
        public Camera ShowcaseCamera;
        public bool FilmMode { get; set; }
        public bool FilmInteraction { get; set; }
        public bool FilmVision { get; set; }
        public void SetFilmLightingBlend(float? blend) => _lighting?.SetFilmBlend(blend);
        public int CurrentStageIndex => _stage;
        public Vector3 StageOrigin => CurrentStage.position;
        public void SetFilmNight(bool night, bool immediate) => _lighting?.SetNight(night, immediate);
        public void SetFilmLook()
        {
            if (_shoreline != null && _shoreline.Mode != Afterlife.Devlog.Shoreline.ShorelineMode.Rounded)
            {
                _hoverTerrain?.Dispose(); _hoverTerrain=null;
                _shoreline.Select(Afterlife.Devlog.Shoreline.ShorelineMode.Rounded);
                if (_tileHover != null) _hoverTerrain=new ShowcaseHoverTerrain(CurrentStage,_tileHover);
                if (_pickSurfaces!=null) _pickIndex.Rebuild(_pickSurfaces);
            }
            if (_tileHover != null) _tileHover.GrassMode=Afterlife.Devlog.Hover.GrassHoverMode.Sway;
            _removalMode=Afterlife.Devlog.Destruction.DisappearMode.Dissolve;
            _damageMarksEnabled=true;
            foreach(var target in _targets) { target.RemovalMode=_removalMode; target.DamageMarksEnabled=true; }
        }
        private Canvas _pixelCanvas;
        private RawImage _pixelImage;
        // 기존 에디터 업그레이드의 After 인덱스(2)는 보존한다.
        public Transform[] Stages;
        public Transform[] PresentationStages;
        [HideInInspector] public Transform[] ArchivedStages;
        public bool HasPresentationStages => PresentationStages != null && PresentationStages.Length == 6;
        public Transform[] StageRoots => HasPresentationStages ? PresentationStages : Stages;
        private Transform CurrentStage => StageRoots[_stage];
        private int Phase => HasPresentationStages ? _stage : (_stage == 2 ? 5 : _stage);
        private static readonly string[] PhaseNames = { "BEFORE / PRIMITIVES", "SHAPE / PIXEL STYLE", "TERRAIN / WATER", "LIVING ENVIRONMENT", "INTERACTION", "AFTER / ATMOSPHERE" };
        public UniversalRenderPipelineAsset Pipeline;
        public Material ParticleMaterial;
        public GameObject Axe, Pick;
        [Range(0, 5)] public int StartStage = 2;
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
        private ShowcaseSmoke _smoke;
        private Afterlife.Devlog.Wind.ShowcaseWind _wind;
        private ShowcaseCrystalGlow _crystalGlow;
        private ShowcaseGrass _grass;
        private readonly List<ShowcaseLeafCards> _treeLeaves=new List<ShowcaseLeafCards>();
        private int LeafCount { get { int count=0; foreach (var leaves in _treeLeaves) count+=leaves.Count; return count; } }
        private bool LeavesEnabled => _treeLeaves.Count>0 && _treeLeaves[0].Enabled;
        private bool LeafRotation => _treeLeaves.Count>0 && _treeLeaves[0].RotationVariation;
        private ShowcaseShorelinePreview _shoreline;
        private Afterlife.Devlog.Hover.ShowcaseTileHover _tileHover;
        private ShowcaseHoverTerrain _hoverTerrain;
        [HideInInspector] public int LayoutVersion;
        [HideInInspector] public int ContinuousTerrainVersion;
        private RenderPipelineAsset _previousGraphics, _previousQuality;
        private UniversalRenderPipelineAsset _ownedPipeline;
        private RenderTexture _texture;
        private Camera _displayCamera;
        private UniversalAdditionalCameraData _refractionCameraData;
        private CameraOverrideOption _previousColorTextureOption;
        private ShowcaseTarget _hover;
        private ShowcaseTarget[] _targets;
        private ShowcasePickSurface[] _pickSurfaces;
        private readonly ShowcasePickIndex _pickIndex=new ShowcasePickIndex();
        private readonly List<Chip> _chips = new List<Chip>();
        private Afterlife.Devlog.Destruction.ShowcaseChipPool _chipPool;
        private MaterialPropertyBlock _chipProperties;
        private const int MaximumChips=256;
        public Afterlife.Devlog.Audio.ShowcaseAudioBank AudioBank;
        public Afterlife.Devlog.AmbientAudio.AmbientAudioSettings Ambience = new Afterlife.Devlog.AmbientAudio.AmbientAudioSettings();
        private Afterlife.Devlog.AmbientAudio.ShowcaseAmbience _ambience;
        private bool _brushingGrass;
        public Afterlife.Devlog.Destruction.DestructionProfile DestructionSettings;
        public GameObject ChipPrefab;
        public ParticleSystem SmokePrefab;
        private Afterlife.Devlog.Audio.ShowcaseAudio _sound;
        private Afterlife.Devlog.Audio.ShowcaseAudioBank _fallbackAudioBank;
        private int _stage = 2;
        private bool _pixels = true;
        private float _shake, _toolAge = 10;
        private Vector3 _toolPoint;
        private GameObject _activeTool;
        private ShowcaseTarget _pendingTarget;
        private Vector3 _approach, _hitLocalPoint;
        private Transform _axeImpactPoint, _pickImpactPoint, _activeImpactPoint, _hitSurface;
        private Quaternion _swingBasis;
        private bool _impactPlayed;
        private Color _impactColor = Color.white;
        private Vector3 _impactNormal = Vector3.up;
        private const float ImpactTime = .12f, ContactHold = Afterlife.Devlog.Destruction.ShowcaseImpactTiming.Hold, SwingDuration = .285f;
        private readonly Vector3 _cameraOffset = new Vector3(10, 13, -16);
        private Rect _display;
        private const float Aspect = 16f / 9;
        private struct Chip { public Transform Transform; public Vector3 Velocity; public float Age; public bool Snow, Tap; public float FloorY; }

        private void OnEnable()
        {
            if (ShowcaseCamera == null || Pipeline == null || Stages == null || Stages.Length != 3)
            { Debug.LogError("Day01: camera, pipeline or stage reference missing.", this); enabled = false; return; }
            // Unity 네이티브 객체는 역직렬화 생성자가 아닌 메인 스레드 수명 콜백에서 생성한다.
            if (_chipProperties==null) _chipProperties=new MaterialPropertyBlock();
            _previousGraphics = GraphicsSettings.defaultRenderPipeline;
            _previousQuality = QualitySettings.renderPipeline;
            _ownedPipeline = Instantiate(Pipeline);
            _ownedPipeline.msaaSampleCount = 1;
            _ownedPipeline.supportsHDR = true;
            _ownedPipeline.supportsCameraDepthTexture = true;
            ShowcaseCamera.GetUniversalAdditionalCameraData().requiresDepthTexture = true;
            _refractionCameraData = ShowcaseCamera.GetUniversalAdditionalCameraData();
            _previousColorTextureOption = _refractionCameraData.requiresColorOption;
            _refractionCameraData.requiresColorTexture = true;
            GraphicsSettings.defaultRenderPipeline = _ownedPipeline;
            QualitySettings.renderPipeline = _ownedPipeline;
            var targets = new List<ShowcaseTarget>();
            foreach (var stage in StageRoots) targets.AddRange(stage.GetComponentsInChildren<ShowcaseTarget>(true));
            _targets = targets.ToArray();
            foreach (var target in _targets) { target.Initialize(); target.DamageMarksEnabled=_damageMarksEnabled; }
            var bank=AudioBank!=null ? AudioBank : Resources.Load<Afterlife.Devlog.Audio.ShowcaseAudioBank>("DevlogAudio/ShowcaseAudioBank");
            if (bank==null)
            {
                // 이전 씬을 처음 임포트하는 동안의 호환 경로. 저장된 오디오 뱅크가 우선이다.
                bank=_fallbackAudioBank=ScriptableObject.CreateInstance<Afterlife.Devlog.Audio.ShowcaseAudioBank>();
                bank.WoodImpact=Resources.Load<AudioClip>("AfterlifePrototype/Audio/01B");
                bank.WoodFall=Resources.Load<AudioClip>("DevlogAudio/gongu_13220749_tree_fall");
                bank.StoneImpacts=new[]{Resources.Load<AudioClip>("DevlogAudio/10_dropping_rocks_impact_17"),Resources.Load<AudioClip>("DevlogAudio/11_dropping_rocks_impact_15")};
                bank.StoneBreak=Resources.Load<AudioClip>("DevlogAudio/dropping_rocks_debris_03");
            }
            _sound=new Afterlife.Devlog.Audio.ShowcaseAudio(transform,bank,
                bank.WaterRipple!=null ? bank.WaterRipple : Resources.Load<AudioClip>("DevlogAudio/freesori_water_ripple"),
                Resources.LoadAll<AudioClip>("DevlogAudio/WaterRipples"));
            _ambience = new Afterlife.Devlog.AmbientAudio.ShowcaseAmbience(transform, Ambience,
                new[] {
                    Resources.Load<AudioClip>("DevlogAmbience/GrassRustles/grass_rustle_02"),
                    Resources.Load<AudioClip>("DevlogAmbience/GrassRustles/grass_rustle_03"),
                    Resources.Load<AudioClip>("DevlogAmbience/GrassRustles/grass_rustle_04")
                },
                Resources.Load<AudioClip>("DevlogAmbience/bird"));
            foreach (var target in _targets) if (DestructionSettings!=null) target.DestructionSettings=DestructionSettings;
            _axeImpactPoint = FindImpactPoint(Axe);
            _pickImpactPoint = FindImpactPoint(Pick);
            _lighting = new ShowcaseLighting(transform,ShowcaseCamera)
            { Height = VisionHeight, CursorRadius = CursorVisionRadius, HouseRadius = HouseVisionRadius, TrailSeconds = VisionTrailSeconds };
            _wind = new Afterlife.Devlog.Wind.ShowcaseWind();
            SetStage(StartStage);
        }

        private void PlayWaterRipple() => _sound?.PlayWaterRipple(Time.unscaledTime);

        private static Vector3? FindChimneyOutlet(Transform house)
        {
            // 현재 House 모델은 굴뚝 입구가 지붕보다 높다. 임포트 축/회전/크기를 모두 반영한다.
            var surfaces = house.GetComponentsInChildren<ShowcasePickSurface>(true);
            float highest = float.NegativeInfinity;
            foreach (var surface in surfaces)
                if (surface.Mesh != null && surface.Mesh.Vertices != null)
                    foreach (var vertex in surface.Mesh.Vertices)
                        highest = Mathf.Max(highest,surface.transform.TransformPoint(vertex).y);
            if (float.IsNegativeInfinity(highest)) return null;
            Vector3 total = Vector3.zero;
            var unique = new HashSet<Vector3>();
            foreach (var surface in surfaces)
                if (surface.Mesh != null && surface.Mesh.Vertices != null)
                    foreach (var vertex in surface.Mesh.Vertices)
                    {
                        Vector3 world = surface.transform.TransformPoint(vertex);
                        if (highest-world.y < .001f && unique.Add(world)) total += world;
                    }
            return unique.Count > 0 ? total/unique.Count : (Vector3?)null;
        }

        private static Transform FindImpactPoint(GameObject tool)
        {
            if (tool == null) return null;
            foreach (var child in tool.GetComponentsInChildren<Transform>(true))
                if (child.name == "ImpactPoint" || child.name.StartsWith("ImpactPoint.")) return child;
            Debug.LogError("Devlog: " + tool.name + "에 ImpactPoint가 없습니다. 도구 FBX를 다시 임포트하세요.", tool);
            return null;
        }

        public void SetStage(int stage)
        {
            ResetTake();
            DisposeStageEffects();
            _stage = Mathf.Clamp(stage, 0, StageRoots.Length - 1);
            for (int i = 0; i < StageRoots.Length; i++) StageRoots[i].gameObject.SetActive(i == _stage);
            _lighting?.SetStage(CurrentStage);
            _lighting?.SetNight(false, true);
            if (Phase == 5)
            {
                _pixels = true;
                _damageMarksEnabled = true;
                _removalMode = Afterlife.Devlog.Destruction.DisappearMode.Dissolve;
                foreach (var target in _targets)
                {
                    target.DamageMarksEnabled = true;
                    target.RemovalMode = _removalMode;
                }
            }
            // 조명 모듈의 기존 창문 마스크는 최종 단계에서만 허용한다.
            foreach (var renderer in CurrentStage.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("House_Mesh")) continue;
                var properties = new MaterialPropertyBlock(); renderer.GetPropertyBlock(properties);
                properties.SetFloat("_HouseWindows", Phase == 5 ? 1 : 0); renderer.SetPropertyBlock(properties);
            }
            CreateStageEffects();
            _pickSurfaces = CurrentStage.GetComponentsInChildren<ShowcasePickSurface>(true);
            _pickIndex.Rebuild(_pickSurfaces);
            ConfigureOutput();
            PositionCamera();
        }

        private void CreateStageEffects()
        {
            if (Phase >= 3)
            {
                _smoke = new ShowcaseSmoke(CurrentStage,FindChimneyOutlet,SmokePrefab,_wind.Sample);
                _grass = new ShowcaseGrass(CurrentStage);
                foreach (var target in _targets)
                {
                    if (!target.Wood || !target.transform.IsChildOf(CurrentStage)) continue;
                    var surfaces=new List<ShowcaseLeafCards.Surface>();
                    foreach (var surface in target.Visual.GetComponentsInChildren<ShowcasePickSurface>(true))
                    {
                        if (surface.SnowCap || surface.Mesh==null) continue;
                        surfaces.Add(new ShowcaseLeafCards.Surface { Transform=surface.transform, Vertices=surface.Mesh.Vertices,
                            Triangles=surface.Mesh.Triangles,Normals=surface.Mesh.Normals,Colors=surface.Mesh.Colors,Renderer=surface.VisibleRenderer });
                    }
                    var leaves=new ShowcaseLeafCards(surfaces,()=>target.RenderVersion);
                    _treeLeaves.Add(leaves);
                    Debug.Log("Leaf sprites: "+target.name+" / "+leaves.Count+" cards.",target);
                }
            }
            if (Phase >= 2)
            {
                _shoreline = new ShowcaseShorelinePreview(CurrentStage);
                if (Phase == 5) _shoreline.Select(Afterlife.Devlog.Shoreline.ShorelineMode.Rounded);
            }
            if (Phase >= 4)
            {
                _tileHover = new Afterlife.Devlog.Hover.ShowcaseTileHover(CurrentStage);
                if (Phase == 5) _tileHover.GrassMode = Afterlife.Devlog.Hover.GrassHoverMode.Sway;
                _tileHover.WaterRippleCreated += PlayWaterRipple;
                _hoverTerrain = new ShowcaseHoverTerrain(CurrentStage, _tileHover);
            }
            if (Phase == 5) _crystalGlow = new ShowcaseCrystalGlow(CurrentStage, ShowcaseCamera);
        }

        private void DisposeStageEffects()
        {
            _pickIndex.Clear(); _pickSurfaces=null;
            if (_tileHover != null) _tileHover.WaterRippleCreated -= PlayWaterRipple;
            _hoverTerrain?.Dispose(); _hoverTerrain = null;
            _tileHover?.Dispose(); _tileHover = null;
            _shoreline?.Dispose(); _shoreline = null;
            foreach (var leaves in _treeLeaves) leaves.Dispose();
            _treeLeaves.Clear();
            _grass?.Dispose(); _grass = null;
            _crystalGlow?.Dispose(); _crystalGlow = null;
            _smoke?.Dispose(); _smoke = null;
        }

        private void ConfigureOutput()
        {
            if (_pixelImage != null) _pixelImage.texture = null;
            if (_pixelCanvas != null) _pixelCanvas.gameObject.SetActive(false);
            if (_texture != null) { ShowcaseCamera.targetTexture = null; _texture.Release(); Destroy(_texture); _texture = null; }
            bool pixelated = _pixels && Phase >= 1;
            if (pixelated && _displayCamera == null)
            {
                var output = new GameObject("PixelDisplayCamera");
                output.transform.SetParent(transform,false);
                _displayCamera = output.AddComponent<Camera>();
                _displayCamera.cullingMask = 0;
                _displayCamera.clearFlags = CameraClearFlags.SolidColor;
                _displayCamera.backgroundColor = Color.black;
                _displayCamera.allowHDR = false; _displayCamera.allowMSAA = false;
                var data = output.AddComponent<UniversalAdditionalCameraData>();
                data.renderPostProcessing = false; data.renderShadows = false;
                data.requiresDepthTexture = false; data.requiresColorTexture = false;
            }
            if (_displayCamera != null)
            {
                // GUI로 확대 표시하는 픽셀 화면 뒤에 실제 Display 출력 경로를 유지한다.
                _displayCamera.targetDisplay = ShowcaseCamera.targetDisplay;
                _displayCamera.depth = ShowcaseCamera.depth + 1;
                _displayCamera.enabled = pixelated;
            }
            // 16:9 고정 구도로 단계/녹화 해상도에 따른 전후 비교 오차를 막는다.
            float height = Mathf.Min(Screen.height, Screen.width / Aspect);
            _display = new Rect((Screen.width - height * Aspect) / 2, (Screen.height - height) / 2, height * Aspect, height);
            ShowcaseCamera.rect = pixelated ? new Rect(0, 0, 1, 1) : new Rect(_display.x / Screen.width, _display.y / Screen.height, _display.width / Screen.width, _display.height / Screen.height);
            ShowcaseCamera.aspect = Aspect;
            if (!pixelated) return;
            int scale = Mathf.Max(1, Mathf.RoundToInt(_display.height / Mathf.Clamp(PixelHeight, 180, 720)));
            int h = Mathf.Max(1, Mathf.RoundToInt(_display.height / scale));
            _texture = new RenderTexture(Mathf.RoundToInt(h * Aspect), h, 24, RenderTextureFormat.DefaultHDR) { name = "Day01 Pixel Frame", filterMode = FilterMode.Point, antiAliasing = 1 };
            _texture.Create();
            ShowcaseCamera.targetTexture = _texture;
            if (_pixelCanvas == null)
            {
                var go = new GameObject("PixelPresentation", typeof(RectTransform), typeof(Canvas));
                go.transform.SetParent(transform,false);
                _pixelCanvas = go.GetComponent<Canvas>();
                _pixelCanvas.renderMode = RenderMode.ScreenSpaceOverlay; _pixelCanvas.sortingOrder = -1000;
                var frame = new GameObject("PixelFrame", typeof(RectTransform), typeof(RawImage));
                frame.transform.SetParent(go.transform,false);
                _pixelImage = frame.GetComponent<RawImage>(); _pixelImage.raycastTarget = false;
            }
            _pixelCanvas.targetDisplay = ShowcaseCamera.targetDisplay;
            _pixelImage.texture = _texture;
            _pixelImage.rectTransform.anchorMin = _pixelImage.rectTransform.anchorMax = Vector2.one*.5f;
            _pixelImage.rectTransform.anchoredPosition = Vector2.zero;
            _pixelImage.rectTransform.sizeDelta = _display.size;
            _pixelCanvas.gameObject.SetActive(true);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && !FilmMode)
            {
                if (keyboard.digit1Key.wasPressedThisFrame) SetStage(0);
                if (keyboard.digit2Key.wasPressedThisFrame) SetStage(1);
                if (keyboard.digit3Key.wasPressedThisFrame) SetStage(2);
                if (HasPresentationStages && keyboard.digit4Key.wasPressedThisFrame) SetStage(3);
                if (HasPresentationStages && keyboard.digit5Key.wasPressedThisFrame) SetStage(4);
                if (HasPresentationStages && keyboard.digit6Key.wasPressedThisFrame) SetStage(5);
                if (keyboard.hKey.wasPressedThisFrame) ShowGuide = !ShowGuide;
                if (keyboard.pKey.wasPressedThisFrame) { _pixels = !_pixels; ConfigureOutput(); }
                if (keyboard.rKey.wasPressedThisFrame) ResetTake();
                if (keyboard.vKey.wasPressedThisFrame) _visionTest=!_visionTest;
                if (keyboard.nKey.wasPressedThisFrame && Phase == 5) _lighting?.Toggle();
                if (keyboard.lKey.wasPressedThisFrame) foreach (var leaves in _treeLeaves) leaves.Toggle();
                if (keyboard.gKey.wasPressedThisFrame) foreach (var leaves in _treeLeaves) leaves.ToggleRotation();
            }
            _lighting?.SetVisionTest(FilmMode ? FilmVision : _visionTest);
            _lighting?.Tick(Time.unscaledDeltaTime);
            _wind?.Tick(Time.unscaledTime, Phase >= 3 ? 1 : 0);
            _smoke?.TickWind();
            float height = Mathf.Min(Screen.height, Screen.width / Aspect);
            if (Mathf.Abs(_display.height - height) > 1 || Mathf.Abs(_display.center.x - Screen.width * .5f) > 1 || Mathf.Abs(_display.center.y - Screen.height * .5f) > 1) ConfigureOutput();
            _shake = Mathf.Max(0, _shake - Time.unscaledDeltaTime);
            PositionCamera();
            UpdatePointer();
            _ambience?.Tick(Time.unscaledDeltaTime, Phase >= 3, _lighting == null || !_lighting.Night, _brushingGrass);
            _tileHover?.Tick(Time.unscaledDeltaTime);
            for (int i = _chips.Count - 1; i >= 0; i--)
            {
                var chip = _chips[i];
                chip.Age += Time.unscaledDeltaTime;
                float lifetime = chip.Tap ? .5f : chip.Snow ? 1.05f : .65f;
                if (chip.Age > lifetime) { _chipPool.Return(chip.Transform.gameObject); _chips.RemoveAt(i); continue; }
                chip.Velocity += Vector3.down * (chip.Tap ? 3 : chip.Snow ? 7.5f : 9) * Time.unscaledDeltaTime;
                chip.Transform.position += chip.Velocity * Time.unscaledDeltaTime;
                if (chip.Snow && chip.Transform.position.y < chip.FloorY+.012f)
                {
                    Vector3 position = chip.Transform.position; position.y = chip.FloorY+.012f;
                    chip.Transform.position = position; chip.Velocity = Vector3.zero;
                    chip.Age = Mathf.Max(chip.Age,lifetime-.16f);
                }
                chip.Transform.Rotate(new Vector3(110, 160, 70) * Time.unscaledDeltaTime);
                chip.Transform.localScale = (chip.Tap ? Vector3.one*.075f : chip.Snow ? new Vector3(.045f,.018f,.045f) : Vector3.one*.075f)
                    * Mathf.Clamp01((lifetime - chip.Age) / .2f);
                _chips[i]=chip;
            }
        }

        // 자원 흔들림의 Update 이후 실제 표면 위치와 날 위치를 맞춘다.
        private void LateUpdate()
        {
            AnimateTool();
            foreach (var leaves in _treeLeaves) leaves.Tick(ShowcaseCamera);
            _grass?.Tick(ShowcaseCamera);
        }

        private void PositionCamera()
        {
            if (FilmMode) return;
            Vector3 focus = CurrentStage.position + Vector3.up * .5f;
            ShowcaseCamera.transform.SetPositionAndRotation(focus + _cameraOffset, Quaternion.LookRotation(-_cameraOffset));
            if (CameraShake && _shake > 0) ShowcaseCamera.transform.position += ShowcaseCamera.transform.right * Mathf.Sin(_shake * 160) * _shake * .2f;
        }

        private void UpdatePointer()
        {
            _brushingGrass = false;
            _tileHover?.SetPointer(null);
            _lighting?.UpdateCursor(null);
            if (_hover != null) _hover.SetHover(false);
            _hover = null;
            if ((Phase < 4 && !_visionTest) || (FilmMode && !FilmInteraction)) return;
            var mouse = Mouse.current;
            if (mouse == null || !Application.isFocused) return;
            Vector2 point = mouse.position.ReadValue();
            Vector2 guiPoint = new Vector2(point.x, Screen.height - point.y);
            if (ShowGuide && GuideRect().Contains(guiPoint)) return;
            if (!_display.Contains(guiPoint)) return;
            Vector3 viewport = new Vector3((point.x - _display.x) / _display.width, (point.y - _display.y) / _display.height, 0);
            if (_texture != null)
            {
                viewport.x = (Mathf.Floor(viewport.x * _texture.width) + .5f) / _texture.width;
                viewport.y = (Mathf.Floor(viewport.y * _texture.height) + .5f) / _texture.height;
            }
            Ray ray = ShowcaseCamera.ViewportPointToRay(viewport);
            _lighting?.UpdateCursor(ray);
            if(Phase<4)return;
            if (!PickSurface(ray, out var surface, out var hitPoint, out var surfaceColor)) return;
            if (Phase>=4 && surface.Target==null && surface.HitNormal.y>.35f
                && (surface.name.StartsWith("Tile_") || surface.name=="OceanBlocks_HalfHeight"))
            {
                Vector3 local=CurrentStage.InverseTransformPoint(hitPoint);
                var biome=Afterlife.Devlog.Terrain.ShowcaseBiomeMap.At(Mathf.FloorToInt(local.x),Mathf.FloorToInt(local.z));
                _tileHover?.SetPointer(hitPoint,biome,mouse.delta.ReadValue().sqrMagnitude>.01f);
                _brushingGrass = mouse.delta.ReadValue().sqrMagnitude>.01f
                    && _grass!=null && _grass.HasGrassAt(hitPoint);
            }
            // 가장 가까운 표면이 건물/폐허면 뒤의 자원을 때리지 않는다.
            _hover = surface.Target as ShowcaseTarget;
            if (_hover == null || !_hover.isActiveAndEnabled)
            {
                if (Phase>=4 && mouse.leftButton.wasPressedThisFrame && _pendingTarget==null)
                    TapFeedback(hitPoint,surface.HitNormal,surfaceColor);
                return;
            }
            _hover.SetHover(true);
            if (!mouse.leftButton.wasPressedThisFrame || !_hover.CanHit || _pendingTarget != null) return;
            _toolPoint = hitPoint;
            _impactColor = surfaceColor;
            _impactNormal = surface.HitNormal.normalized;
            if (_activeTool != null) _activeTool.SetActive(false);
            _activeTool = _hover.Wood ? Axe : Pick;
            _activeImpactPoint = _hover.Wood ? _axeImpactPoint : _pickImpactPoint;
            if (!_hover.Juicy || _activeTool == null || _activeImpactPoint == null) { ApplyImpact(_hover, hitPoint); return; }
            // クリック時の交差点を固定し、以後のマウス移動で照準を変えない。
            _pendingTarget = _hover;
            _hitSurface = surface.transform;
            _hitLocalPoint = _hitSurface.InverseTransformPoint(hitPoint);
            _swingBasis = ShowcaseCamera.transform.rotation;
            _approach = ShowcaseCamera.transform.right * .45f + ShowcaseCamera.transform.up * .55f - ShowcaseCamera.transform.forward * .25f;
            _toolAge = 0;
            _impactPlayed = false;
            _activeTool.SetActive(true);
        }

        private GameObject CreateChip()
        {
            if (_chipPool==null) _chipPool=new Afterlife.Devlog.Destruction.ShowcaseChipPool(transform,ChipPrefab);
            if (_chips.Count>=MaximumChips) { _chipPool.Return(_chips[0].Transform.gameObject); _chips.RemoveAt(0); }
            return _chipPool.Rent();
        }

        private void TapFeedback(Vector3 point,Vector3 normal,Color color)
        {
            Vector3 tangent=Vector3.Cross(normal,Mathf.Abs(normal.y)>.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent=Vector3.Cross(normal,tangent);
            for (int i=0;i<4;i++)
            {
                var go=CreateChip(); go.name="ClickChip";
                go.transform.SetParent(transform,false); go.transform.position=point+normal*.012f;
                go.transform.localScale=Vector3.one*.075f;
                var renderer=go.GetComponent<Renderer>(); renderer.sharedMaterial=ParticleMaterial;
                _chipProperties.Clear(); _chipProperties.SetVector("_BaseColor",(Vector4)color); renderer.SetPropertyBlock(_chipProperties);
                float angle=i*2.39996f;
                _chips.Add(new Chip { Transform=go.transform,Tap=true,Velocity=normal*(.7f+i*.05f)+Vector3.up*.5f
                    +(tangent*Mathf.Cos(angle)+bitangent*Mathf.Sin(angle))*.48f });
            }
        }

        private void ApplyImpact(ShowcaseTarget target, Vector3 hitPoint)
        {
            if (!target.CanHit) return;
            Vector3 incoming = _pendingTarget == target ? -_approach : ShowcaseCamera.transform.forward;
            var snowPositions = target.ShedSnow(hitPoint);
            target.RecordDamageMark(hitPoint,_impactNormal,_impactColor);
            target.Hit(hitPoint,incoming);
            _sound?.Play(target.Wood,target.IsBroken,Time.unscaledTime);
            if (!target.Juicy) return;
            _shake = .14f;
            int debrisCount=target.IsBroken ? 24 : 9;
            Vector3 outward = _impactNormal.sqrMagnitude>.01f ? _impactNormal : -incoming.normalized;
            Vector3 tangent=Vector3.Cross(outward,Mathf.Abs(outward.y)>.9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent=Vector3.Cross(outward,tangent);
            for (int i = 0; i < debrisCount+snowPositions.Count; i++)
            {
                bool snow = i >= debrisCount;
                var go = CreateChip();
                go.name = snow ? "SnowChip" : "HitChip";
                go.transform.SetParent(transform, false);
                go.transform.position = snow ? snowPositions[i-debrisCount] : hitPoint+outward*.012f;
                go.transform.localScale = snow ? new Vector3(.045f,.018f,.045f) : Vector3.one*.075f;
                var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = ParticleMaterial;
                var block = _chipProperties; block.Clear();
                // 색 공간 변환을 마친 표면색을 그대로 전달한다(이중 감마 변환 방지).
                block.SetVector("_BaseColor",snow ? new Vector4(.87f,.94f,1,1) : (Vector4)_impactColor); renderer.SetPropertyBlock(block);
                float a = i * 2.39996f;
                _chips.Add(new Chip { Transform = go.transform, Snow = snow, FloorY = target.transform.position.y, Velocity = snow
                    ? new Vector3(Mathf.Cos(a)*.40f,-.6f-((i-debrisCount)%7)*.12f,Mathf.Sin(a)*.40f)
                    : outward*(1.2f+(i%3)*.18f)+Vector3.up*.85f
                        +(tangent*Mathf.Cos(a)+bitangent*Mathf.Sin(a))*(.5f+(i%4)*.13f) });
            }
        }

        private bool PickSurface(Ray ray, out ShowcasePickSurface nearest, out Vector3 point, out Color color)
        {
            return _pickIndex.Pick(ray,ShowcaseCamera.farClipPlane,out nearest,out point,out color);
        }

        private void AnimateTool()
        {
            if (_pendingTarget == null || !_pendingTarget.isActiveAndEnabled || _activeTool == null || _activeImpactPoint == null || _hitSurface == null)
            {
                if (_activeTool != null) _activeTool.SetActive(false);
                _pendingTarget = null;
                return;
            }
            _toolAge += Time.unscaledDeltaTime;
            bool contactNow = !_impactPlayed && _toolAge >= ImpactTime;
            // 느린 프레임에서도 접촉 포즈를 한 번은 표시한 뒤 회수한다.
            if (contactNow) _toolAge = ImpactTime;
            if (_toolAge >= SwingDuration)
            { _activeTool.SetActive(false); _pendingTarget = null; return; }
            _toolPoint = _hitSurface.TransformPoint(_hitLocalPoint);
            float approach = Mathf.Clamp01(_toolAge / ImpactTime);
            approach *= approach; // 접촉 직전까지 가속하고 접촉 포즈에서 짧게 정지한다.
            float recovery = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(ImpactTime + ContactHold, SwingDuration, _toolAge));
            Vector3 contactPosition = _toolPoint + _approach * (1 - approach + recovery * .65f);
            float angle = Mathf.Lerp(-65, 25, approach) - recovery * 35;
            _activeTool.transform.rotation = _swingBasis * Quaternion.Euler(0, 0, angle);
            // FBX 안의 실제 날 마커를 월드 공간에서 직접 정렬한다.
            // 중간 부모의 회전/축 보정/스케일과 무관하게 marker.position == contactPosition이다.
            _activeTool.transform.position += contactPosition - _activeImpactPoint.position;
            if (contactNow)
            {
                _impactPlayed = true;
                ApplyImpact(_pendingTarget, _toolPoint);
            }
        }

        public void ResetTake()
        {
            ResetTake(true);
        }

        private void ResetTake(bool resetAmbience)
        {
            _lighting?.ResetVisionTrail();
            _tileHover?.Reset();
            _sound?.Reset();
            if (resetAmbience) _ambience?.Reset();
            _smoke?.ResetTake();
            if (_targets != null) foreach (var target in _targets) target.ResetTake();
            foreach (var chip in _chips) if (chip.Transform != null) _chipPool?.Return(chip.Transform.gameObject);
            _chips.Clear();
            if (Axe != null) Axe.SetActive(false);
            if (Pick != null) Pick.SetActive(false);
            _hover = null; _shake = 0; _toolAge = 10;
            _pendingTarget = null; _impactPlayed = false;
            _hitSurface = null; _activeImpactPoint = null;
            _impactColor = Color.white;
            _impactNormal = Vector3.up;

        }

        private Afterlife.Devlog.Destruction.DisappearMode _removalMode;
        private void SelectRemoval(Afterlife.Devlog.Destruction.DisappearMode mode)
        {
            _removalMode=mode;
            ResetTake();
            foreach (var target in _targets) target.RemovalMode=mode;
        }
        private const float GuideScale = 1.5f;
        private bool _damageMarksEnabled=true;
        private Rect GuideRect() => new Rect(16, 16, 550 * GuideScale, 342 * GuideScale);
        private void OnGUI()
        {
            if (ShowcaseCamera == null) return;
            GUI.depth = 100;
            GUI.color = Color.black;
            if (_texture == null)
            {
                GUI.DrawTexture(new Rect(0, 0, Screen.width, _display.y), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, Screen.height - _display.y, Screen.width, _display.y), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, 0, _display.x, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(Screen.width - _display.x, 0, _display.x, Screen.height), Texture2D.whiteTexture);
            }
            GUI.color = Color.white;
            if (!ShowGuide || FilmMode) return;
            var previousMatrix = GUI.matrix;
            try
            {
                // 화면 출력은 그대로 두고 안내 패널만 좌상단 여백 16px 기준으로 확대한다.
                GUI.matrix = previousMatrix * Matrix4x4.TRS(new Vector3(16*(1-GuideScale),16*(1-GuideScale),0),
                    Quaternion.identity,new Vector3(GuideScale,GuideScale,1));
                DrawGuide();
            }
            finally { GUI.matrix = previousMatrix; }
        }

        private void DrawGuide()
        {
            GUI.Box(new Rect(16,16,550,342), "");
            string[] names = PhaseNames;
            GUI.Label(new Rect(28, 23, 520, 22), "DAY 01   /   " + names[Phase]);
            GUI.Label(new Rect(28, 48, 520, 22), HasPresentationStages ? "1~6 Stages | H Hide guide | F8 Film mode" : "1 Before  2 Process  3 After | H Hide");
            GUI.Label(new Rect(28, 73, 520, 22), "P Pixels   R Reset   G Leaf rotation: "+(LeafRotation ? "Random 360" : "Uniform"));
            GUI.Label(new Rect(28, 98, 520, 22), (Phase == 5 ? "N Day/Night" : "Day/Night: stage 6")+" | L Leaves: " + (LeavesEnabled ? "ON " : "OFF ")+_treeLeaves.Count+" trees / "+LeafCount+" cards");
            GUI.enabled = Phase >= 4;
            if (GUI.Button(new Rect(28,126,180,27),(_removalMode==Afterlife.Devlog.Destruction.DisappearMode.Sink ? "[Selected] " : "")+"Sink + reset"))
                SelectRemoval(Afterlife.Devlog.Destruction.DisappearMode.Sink);
            if (GUI.Button(new Rect(218,126,200,27),(_removalMode==Afterlife.Devlog.Destruction.DisappearMode.Dissolve ? "[Selected] " : "")+"Dissolve + reset"))
                SelectRemoval(Afterlife.Devlog.Destruction.DisappearMode.Dissolve);
            GUI.enabled = Phase >= 2;
            string[] shoreNames={"Square","Bevel","Rounded"};
            for (int i=0;i<3;i++)
            {
                var mode=(Afterlife.Devlog.Shoreline.ShorelineMode)i;
                if (GUI.Button(new Rect(28+i*170,161,160,27),(_shoreline!=null && _shoreline.Mode==mode ? "* " : "")+"Coast: "+shoreNames[i]))
                {
                    _hoverTerrain?.Dispose(); _hoverTerrain=null;
                    _shoreline?.Select(mode);
                    if (_tileHover!=null) { _tileHover.Reset(); _hoverTerrain=new ShowcaseHoverTerrain(CurrentStage,_tileHover); }
                    if (_pickSurfaces!=null) _pickIndex.Rebuild(_pickSurfaces);
                }
            }
            GUI.enabled = Phase >= 4;
            if (GUI.Button(new Rect(28,196,245,27),(_tileHover!=null && _tileHover.GrassMode==Afterlife.Devlog.Hover.GrassHoverMode.Radial ? "* " : "")+"Grass: Radial bend"))
            { _tileHover?.Reset(); if (_tileHover!=null) _tileHover.GrassMode=Afterlife.Devlog.Hover.GrassHoverMode.Radial; }
            if (GUI.Button(new Rect(283,196,245,27),(_tileHover!=null && _tileHover.GrassMode==Afterlife.Devlog.Hover.GrassHoverMode.Sway ? "* " : "")+"Grass: Sway + recover"))
            { _tileHover?.Reset(); if (_tileHover!=null) _tileHover.GrassMode=Afterlife.Devlog.Hover.GrassHoverMode.Sway; }
            if (GUI.Button(new Rect(28,233,500,27),"Damage marks: "+(_damageMarksEnabled ? "ON" : "OFF")+"  / compare same hits"))
            {
                _damageMarksEnabled=!_damageMarksEnabled;
                foreach (var target in _targets) target.DamageMarksEnabled=_damageMarksEnabled;
            }
            GUI.enabled = true;
            _visionTest=GUI.Toggle(new Rect(28,307,500,27),_visionTest,"V Mouse vision test: "+(_visionTest?"ON":"OFF"));
            string[] buttons = HasPresentationStages ? new[] { "1 Before", "2 Shape", "3 Terrain", "4 Life", "5 Hits", "6 After" } : new[] { "1 Before", "2 Process", "3 After" };
            for (int i = 0; i < buttons.Length; i++)
                if (GUI.Button(new Rect(28 + i * 86, 270, 82, 27), (_stage == i ? "* " : "") + buttons[i])) SetStage(i);
        }

        private void OnDisable()
        {
            if (_pixelCanvas != null) { _pixelCanvas.gameObject.SetActive(false); Destroy(_pixelCanvas.gameObject); _pixelCanvas=null; _pixelImage=null; }
            if (_tileHover!=null) _tileHover.WaterRippleCreated-=PlayWaterRipple;
            _sound?.Dispose(); _sound=null;
            _ambience?.Dispose(); _ambience=null;
            if (_fallbackAudioBank!=null) { Destroy(_fallbackAudioBank); _fallbackAudioBank=null; }
            DisposeStageEffects();
            _wind?.Dispose(); _wind = null;
            _lighting?.Dispose(); _lighting = null;
            ResetTake();
            _chipPool?.Dispose(); _chipPool=null;
            if (_refractionCameraData != null)
            {
                _refractionCameraData.requiresColorOption = _previousColorTextureOption;
                _refractionCameraData = null;
            }
            if (ShowcaseCamera != null) { ShowcaseCamera.targetTexture = null; ShowcaseCamera.rect = new Rect(0, 0, 1, 1); }
            if (_texture != null) { _texture.Release(); Destroy(_texture); _texture = null; }
            if (_displayCamera != null)
            {
                _displayCamera.enabled = false;
                Destroy(_displayCamera.gameObject); _displayCamera = null;
            }
            if (_ownedPipeline != null)
            {
                if (GraphicsSettings.defaultRenderPipeline == _ownedPipeline) GraphicsSettings.defaultRenderPipeline = _previousGraphics;
                if (QualitySettings.renderPipeline == _ownedPipeline) QualitySettings.renderPipeline = _previousQuality;
                Destroy(_ownedPipeline); _ownedPipeline = null;
            }
        }
    }
}
