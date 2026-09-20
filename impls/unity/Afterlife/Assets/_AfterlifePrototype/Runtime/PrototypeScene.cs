using System.Linq;
using Afterlife.Modules.Progression;
using Moonstone.Arc.DependencyInjection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Afterlife.Prototype
{
    public sealed class PrototypeScene : MonoBehaviour
    {
        public const string SceneName = "Afterlife_PixelPrototype";
        private RenderPipelineAsset _previousGraphics, _previousQuality;
        private UniversalRenderPipelineAsset _pipeline;
        private UniversalRendererData _renderer;
        private PrototypeSettings _settings;
        private ServiceResolver _resolver;
        private bool _ownsPipeline, _ownsSettings;
        private ProgressionCatalog _ownedProgression;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Register()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != SceneName) return;
            foreach (var root in scene.GetRootGameObjects())
                if (root.GetComponent<PrototypeScene>() != null) return;
            Debug.LogError("Prototype 씬에 GameRoot가 없습니다. 저장된 씬의 GameRoot와 PrototypeScene 컴포넌트를 확인하세요.");
        }
        private void Awake()
        {
            _settings = Resources.Load<PrototypeSettings>("AfterlifePrototype/Settings");
            if (_settings == null) { _settings = ScriptableObject.CreateInstance<PrototypeSettings>(); _ownsSettings = true; }
            var sourcePipeline = Resources.Load<UniversalRenderPipelineAsset>("AfterlifePrototype/Pipeline");
            if (sourcePipeline != null) { _pipeline = Instantiate(sourcePipeline); _ownsPipeline = true; }
#if UNITY_EDITOR
            if (_pipeline == null)
            {
                _renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                ResourceReloader.ReloadAllNullIn(_renderer, UniversalRenderPipelineAsset.packagePath);
                _pipeline = UniversalRenderPipelineAsset.Create(_renderer);
                _ownsPipeline = true;
            }
#endif
            if (_pipeline == null)
            {
                Debug.LogError("Afterlife: Resources/AfterlifePrototype/Pipeline 렌더 에셋이 없습니다. 프로젝트 에셋 참조를 확인하세요.");
                enabled = false; return;
            }
            _pipeline.msaaSampleCount = 1;
            _pipeline.supportsHDR = true;
            ConfigureWaterRendering();
            _previousGraphics = GraphicsSettings.defaultRenderPipeline;
            _previousQuality = QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline = _pipeline;
            QualitySettings.renderPipeline = _pipeline;
            var model = new VillageModel(_settings.ToBalance());
            var view = GetComponent<PrototypeView>();
            if(view==null){Debug.LogError("PrototypeView 씬 참조 누락");return;}
            var buildCatalog=view.SceneUi!=null&&view.SceneUi.BuildCatalog!=null?view.SceneUi.BuildCatalog:Resources.Load<PrototypeBuildCatalog>(PrototypeBuildCatalog.ResourcePath);
            if(buildCatalog!=null)
            {
                var rules=new System.Collections.Generic.List<ConstructionRule>();
                foreach(var data in buildCatalog.Buildings)if(data!=null)rules.Add(data.ToRule());
                model.ConfigureConstruction(rules);
            }
            var progression = Resources.Load<ProgressionCatalog>(PrototypeProgressionDefaults.ResourcePath);
            if (progression != null) { progression = Instantiate(progression); _ownedProgression = progression; }
            if (progression == null)
            {
                progression = ScriptableObject.CreateInstance<ProgressionCatalog>();
                PrototypeProgressionDefaults.Populate(progression); _ownedProgression = progression;
                Debug.LogWarning("강화 데이터 에셋이 없어 기본 데이터를 사용합니다. 강화 에디터에서 에셋 생성을 확인하세요.");
            }
            var errors = PrototypeProgressionDefaults.Validate(progression);
            if (errors.Count > 0) { Debug.LogError("강화 데이터 오류: " + string.Join(" / ", errors)); enabled = false; return; }
            model.ConfigureProgression(progression.Nodes.Select(node => node.ToDefinition()), progression.Skills.Select(skill => skill.ToDefinition()),
                progression.Trees.SelectMany(tree => tree.Nodes.Select(node => new ProgressionRule { NodeId = node.Id, TreeId = tree.Id, EffectId = node.EffectId, SkillId = node.SkillId, Values = node.Levels.Select(level => level.Value).ToArray() })));
            view.ProgressionCatalog = progression;
            // 새 씬 단위 Moonstone 조립. 기존 전역 컨테이너/레거시 ModeSystem을 변경하지 않는다.
            _resolver = new ServiceResolver();
            var gameFlow = new PrototypeGameFlow(view);
            _resolver.Register(model);
            _resolver.Register(_settings);
            _resolver.Register(view);
            _resolver.Register(model.Upgrades);
            _resolver.Register(model.Skills);
            _resolver.Register(gameFlow);
            _resolver.BindEachOther();
            view.SetUp();
        }
        private void ConfigureWaterRendering()
        {
            _pipeline.supportsCameraDepthTexture = true;
            _pipeline.supportsCameraOpaqueTexture = true;
            if (_renderer == null)
            {
                // URP 17은 런타임 renderer 목록 교체 API가 없다. 저장 에셋 대신 복사본만 수정한다.
                var field = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var renderers = field?.GetValue(_pipeline) as ScriptableRendererData[];
                if (renderers == null || renderers.Length != 1 || !(renderers[0] is UniversalRendererData source))
                    throw new System.InvalidOperationException("Prototype water requires one UniversalRendererData (URP 17).");
                _renderer = Instantiate(source);
                field.SetValue(_pipeline, new ScriptableRendererData[] { _renderer });
            }
            _renderer.copyDepthMode = CopyDepthMode.AfterOpaques;
            _renderer.depthPrimingMode = DepthPrimingMode.Disabled;
        }
        private void OnDestroy()
        {
            _resolver?.Dispose();
            _resolver = null;
            if (_pipeline != null && GraphicsSettings.defaultRenderPipeline == _pipeline)
            {
                GraphicsSettings.defaultRenderPipeline = _previousGraphics;
                QualitySettings.renderPipeline = _previousQuality;
            }
            if (_ownsPipeline) { Destroy(_pipeline); Destroy(_renderer); }
            if (_ownsSettings) Destroy(_settings);
            if (_ownedProgression != null) Destroy(_ownedProgression);
        }
    }
}
