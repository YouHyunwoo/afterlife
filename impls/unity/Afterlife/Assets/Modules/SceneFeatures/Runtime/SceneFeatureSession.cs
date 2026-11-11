using System;
using System.Collections.Generic;
using UnityEngine;
using Afterlife.Modules.Terrain;
using Afterlife.Modules.TerrainView;
using Afterlife.Modules.Interaction;
using Afterlife.Modules.Vegetation;
using Afterlife.Modules.Hover;
using Afterlife.Modules.Lighting;
namespace Afterlife.Modules.SceneFeatures
{
    // 일반 C# 진입점. MonoBehaviour/Editor 없이도 생성과 갱신을 호출할 수 있다.
    public sealed class SceneFeatureSession : IDisposable
    {
        private static SceneFeatureSession _activeEnvironment;
        private readonly SceneFeatureProfile _profile;
        private readonly SceneBindings _bindings;
        private readonly Transform _owner;
        private readonly SceneFeatureSettings _settings;
        private readonly List<Action> _release=new List<Action>();
        private readonly List<LeafCardRenderer> _leaves=new List<LeafCardRenderer>();
        private readonly List<GameObject> _externalGenerated=new List<GameObject>();
        private readonly PickSurfaceIndex _picks=new PickSurfaceIndex();
        private readonly List<FeedbackTarget> _targets=new List<FeedbackTarget>();
        private TerrainSurface _terrain;
        private GrassRenderer _grass;
        private TerrainHoverEffects _hover;
        private Afterlife.Modules.Wind.WindFieldController _wind;
        private Afterlife.Modules.WorldAudio.WorldAudioPlayer _sound;
        private Afterlife.Modules.AmbientAudio.AmbientAudioController _ambient;
        private ImpactPresentation _impact;
        private EnvironmentLighting _lighting;
        private CameraPresentation _output;
        private CameraShakePresentation _shake;
        private FeedbackTarget _hovered;
        private float _time;
        private bool _disposed,_brushing,_paused;
        public readonly List<string> Diagnostics=new List<string>();
        public GameObject GeneratedRoot { get; private set; }
        public MapData Map { get; private set; }
        public TerrainGeometry Geometry { get; private set; }
        public IReadOnlyList<GameObject> ExternalGenerated => _externalGenerated;
        public SceneFeatureSettings Settings=>_settings.Copy();
        public bool IsSwinging=>_impact!=null&&_impact.IsSwinging;
        public event Action<FeedbackTarget,Vector3> Contact;
        public SceneFeatureSession(Transform owner,SceneFeatureProfile profile,SceneFeatureSettings settings,SceneBindings bindings,bool activeEnvironment)
        {
            _owner=owner;_profile=profile;_bindings=bindings??new SceneBindings();_settings=(settings??new SceneFeatureSettings()).Copy();
            GeneratedRoot=new GameObject("SceneFeatureGenerated");GeneratedRoot.transform.SetParent(owner,false);GeneratedRoot.hideFlags=HideFlags.DontSave;
            try { Build(activeEnvironment); }
            catch { Dispose();throw; }
        }
        private bool On(SceneFeature feature)=>_settings.IsEnabled(feature);
        private bool Need(SceneFeature feature,bool valid,string message) { if(!On(feature))return false;if(!valid)Diagnostics.Add(feature+": "+message);return valid; }
        private bool OwnEnvironment(bool requested)
        {
            if(!requested)return false;
            if(_activeEnvironment!=null && _activeEnvironment!=this) { Diagnostics.Add("활성 환경이 이미 있습니다. 전역 효과는 보류합니다.");return false; }
            _activeEnvironment=this;return true;
        }
        private void Build(bool activeEnvironment)
        {
            if(_profile==null) { Diagnostics.Add("프로필이 필요합니다.");return; }
            bool needsGlobal=On(SceneFeature.Wind)||On(SceneFeature.TerrainReaction)||On(SceneFeature.DayNight)||On(SceneFeature.Vision)||On(SceneFeature.Pixels)||On(SceneFeature.VisionBlur)||On(SceneFeature.Bloom);
            bool global=OwnEnvironment(activeEnvironment&&needsGlobal);
            try {
                if(On(SceneFeature.MapGeneration)) { var mapSettings=_settings.Map;mapSettings.Beach=On(SceneFeature.Beach);Map=MapGenerator.Generate(mapSettings); }
                else if(_bindings.Map!=null)Map=_bindings.Map.Data.Copy();
            } catch(ArgumentException error) { Diagnostics.Add("맵 데이터: "+error.Message); }
            if(Map!=null&&(_settings.Palette==null||_settings.Palette.Length<7)){Diagnostics.Add("맵 표현: 7개 바이옴 색이 필요합니다.");Map=null;}
            var root=GeneratedRoot.transform;
            if(Map!=null)
            {
                var coast=On(SceneFeature.Coast)?new CoastalTerrain(_settings.CoastShape,_settings.WaterHeight,_settings.FloorHeight):new CoastalTerrain(CoastShape.Square,_settings.WaterHeight,_settings.FloorHeight);
                Geometry=new TerrainGeometry(Map.Radius,Map.At,_settings.Palette,coast,Map.UseHeightSamples?(Func<float,float,float>)((x,z)=>(float)Map.Height(x,z)):null);
                if(Need(SceneFeature.Terrain,_profile.TerrainMaterial!=null,"지형 재질이 필요합니다.")) { _terrain=new TerrainSurface(root,Geometry,_profile.TerrainMaterial);_release.Add(_terrain.Dispose); }
                if(Need(SceneFeature.Water,_profile.WaterShader!=null,"물 셰이더가 필요합니다.")) { var water=new Afterlife.Modules.Water.WaterSurface(root,Map.Radius,(x,z)=>Map.At(x,z)==TerrainBiome.Water,_profile.WaterShader,_settings.WaterHeight,_settings.FloorHeight);water.SetHoverRipples(global&&On(SceneFeature.TerrainReaction));water.SetOpenSeaFoam(_settings.OpenSeaFoam);_release.Add(water.Dispose); }
                if(Need(SceneFeature.Grass,_profile.GrassShader!=null,"풀 셰이더가 필요합니다.")) {
                    _grass=new GrassRenderer(root,new GrassRenderer.Map { Radius=Map.Radius,SamplesPerAxis=_settings.GrassSamples,AccentFraction=On(SceneFeature.Decorations)?1:0,Biome=Map.At,Height=Geometry.Height,Color=Geometry.SurfaceColor },_profile.GrassShader);
                    _grass.SetSizeScale(_settings.GrassSize);_release.Add(_grass.Dispose);
                }
                if(On(SceneFeature.Decorations) && _profile.DecorationPrefabs.Length>0) {
                    var radius=_settings.DecorationRadius>0?Mathf.Min(Map.Radius,_settings.DecorationRadius):Map.Radius;
                    var placements=SurfaceScatter.Generate(radius,_settings.DecorationCount,_settings.Map.Seed,Map.At,(x,z)=>Geometry.Height((float)x,(float)z),_settings.DecorationBiome);
                    for(int i=0;i<placements.Length;i++) {
                        var prefab=_profile.DecorationPrefabs[i%_profile.DecorationPrefabs.Length];if(prefab==null)continue;var item=placements[i];
                        var go=UnityEngine.Object.Instantiate(prefab,root);go.transform.localPosition=new Vector3((float)item.X,(float)item.Y,(float)item.Z);go.transform.localRotation=Quaternion.Euler(0,(float)item.Yaw,0);go.transform.localScale*=(float)item.Scale;
                    }
                }
                if(global && On(SceneFeature.TerrainReaction)) {
                    _hover=new TerrainHoverEffects(root,new TerrainHoverEffects.Map { MinX=-Map.Radius,MinZ=-Map.Radius,Width=Map.Size,Depth=Map.Size,OriginOffset=new Vector3(-.5f,0,-.5f),Biome=Map.At });
                    _hover.GrassMode=GrassHoverMode.Sway;_release.Add(_hover.Dispose);
                }
            }
            else foreach(var f in new[]{SceneFeature.Terrain,SceneFeature.Water,SceneFeature.Grass,SceneFeature.Decorations,SceneFeature.TerrainReaction})if(On(f))Diagnostics.Add(f+": 맵 생성 또는 맵 데이터 연결이 필요합니다.");
            foreach(var target in _bindings.Targets)
            {
                if(target==null)continue;_targets.Add(target);
                bool oldTick=target.ExternalTick,oldMarks=target.DamageMarksEnabled,oldSnow=target.EnableSnow,oldHit=target.HitFeedbackEnabled,oldDestroy=target.DestructionEnabled;
                var oldGround=target.GroundHeightQuery;var oldBiome=target.BiomeQuery;var oldDestruction=target.DestructionSettings;
                target.ExternalTick=true;target.DamageMarksEnabled=On(SceneFeature.DamageMarks);target.EnableSnow=On(SceneFeature.Snow);target.HitFeedbackEnabled=On(SceneFeature.HitFeedback);target.DestructionEnabled=On(SceneFeature.Destruction);
                if(_profile.Destruction!=null)target.DestructionSettings=_profile.Destruction;
                if(Map!=null) { target.GroundHeightQuery=world=>{var local=_owner.InverseTransformPoint(world);return _owner.TransformPoint(new Vector3(local.x,Geometry.Height(local.x,local.z),local.z)).y;};target.BiomeQuery=world=>{var local=_owner.InverseTransformPoint(world);return Map.At(Mathf.FloorToInt(local.x+.5f),Mathf.FloorToInt(local.z+.5f));}; }
                target.Initialize();target.SetFeatureFlags(On(SceneFeature.HitFeedback),On(SceneFeature.DamageMarks),On(SceneFeature.Snow),On(SceneFeature.Destruction));target.Contact+=OnContact;
                foreach(var cap in target.Visual.GetComponentsInChildren<PickSurface>(true))if(cap.SnowCap) {cap.gameObject.hideFlags=HideFlags.DontSave;_externalGenerated.Add(cap.gameObject);}
                _release.Add(()=>{if(target==null)return;target.Contact-=OnContact;target.ReleasePresentation();target.ExternalTick=oldTick;target.DamageMarksEnabled=oldMarks;target.EnableSnow=oldSnow;target.HitFeedbackEnabled=oldHit;target.DestructionEnabled=oldDestroy;target.GroundHeightQuery=oldGround;target.BiomeQuery=oldBiome;target.DestructionSettings=oldDestruction;});
            }
            if(On(SceneFeature.Leaves)) foreach(var filter in _bindings.FoliageSurfaces)
            {
                if(filter==null || filter.sharedMesh==null || !filter.sharedMesh.isReadable) { Diagnostics.Add("Leaves: 읽기 가능한 수관 MeshFilter가 필요합니다.");continue; }
                var before=new HashSet<Transform>(filter.GetComponentsInChildren<Transform>(true));var mesh=filter.sharedMesh;
                var leaf=new LeafCardRenderer(new[]{new LeafCardRenderer.Surface { Transform=filter.transform,Vertices=mesh.vertices,Normals=mesh.normals,Triangles=mesh.triangles,Colors=mesh.colors,Renderer=filter.GetComponent<Renderer>() }},shader:_profile.LeafShader,maxCards:_settings.LeafCards);
                _leaves.Add(leaf);_release.Add(leaf.Dispose);
                foreach(var t in filter.GetComponentsInChildren<Transform>(true))if(!before.Contains(t)){t.gameObject.hideFlags=HideFlags.DontSave;_externalGenerated.Add(t.gameObject);}
            }
            if(global && On(SceneFeature.Wind)) { _wind=new Afterlife.Modules.Wind.WindFieldController();_release.Add(_wind.Dispose); }
            if(Need(SceneFeature.Sound,_profile.AudioBank!=null,"효과음 뱅크가 필요합니다.")) { _sound=new Afterlife.Modules.WorldAudio.WorldAudioPlayer(root,_profile.AudioBank,_profile.AudioBank.WaterRipple);_release.Add(_sound.Dispose);if(_hover!=null)_hover.WaterRippleCreated+=()=>_sound.PlayWaterRipple(_time); }
            if(Need(SceneFeature.Ambience,_profile.GrassSounds.Length>0&&_profile.Bird!=null,"풀 접촉음과 새소리가 필요합니다.")) { _ambient=new Afterlife.Modules.AmbientAudio.AmbientAudioController(root,_settings.Ambient,_profile.GrassSounds,_profile.Bird);_release.Add(_ambient.Dispose); }
            if(On(SceneFeature.Picking)||On(SceneFeature.ClickParticles)||On(SceneFeature.Tools)||On(SceneFeature.HitFeedback)||On(SceneFeature.Sound)) {
                if(_bindings.Camera==null)Diagnostics.Add("피드백: 카메라가 필요합니다.");
                else {
                    _impact=new ImpactPresentation(root,_bindings.Camera,_bindings.Axe,_bindings.Pick,_profile.ChipPrefab,_profile.ParticleMaterial,_sound,_bindings.AxeContact,_bindings.PickContact) {
                        ToolsEnabled=On(SceneFeature.Tools),ClickParticlesEnabled=On(SceneFeature.ClickParticles)&&_profile.ParticleMaterial!=null,HitParticlesEnabled=On(SceneFeature.HitFeedback)&&_profile.ParticleMaterial!=null,SoundEnabled=On(SceneFeature.Sound),ImpactTime=Mathf.Max(.01f,_settings.ToolContactSeconds),SwingDuration=Mathf.Max(_settings.ToolContactSeconds+.03f,_settings.ToolSwingSeconds) };
                    _release.Add(_impact.Dispose);
                    if(On(SceneFeature.HitFeedback)){_shake=new CameraShakePresentation(_bindings.Camera);_release.Add(_shake.Dispose);}
                    if(On(SceneFeature.Tools)&&(_bindings.AxeContact==null&&_bindings.PickContact==null))Diagnostics.Add("Tools: 도구 접촉 지점이 없어 즉시 접촉을 사용합니다.");
                }
            }
            var picking=new List<PickSurface>(_bindings.PickSurfaces);
            foreach(var target in _targets) foreach(var cap in target.Visual.GetComponentsInChildren<PickSurface>(true))if(cap.SnowCap && !picking.Contains(cap))picking.Add(cap);
            _picks.Rebuild(picking.ToArray());
            if(global && (On(SceneFeature.DayNight)||On(SceneFeature.Vision)) && _bindings.Camera!=null) {
                float radius=Map!=null?Map.Radius:_settings.Map.Radius;
                _lighting=new EnvironmentLighting(root,_bindings.Camera,new Rect(-radius,-radius,radius*2,radius*2),_bindings.Sun,_bindings.Windows) {
                    Height=_settings.VisionHeight,CursorRadius=_settings.CursorRadius,HouseRadius=_settings.SourceRadius,TrailSeconds=_settings.TrailSeconds,DayNightEnabled=On(SceneFeature.DayNight),NightSky=_settings.NightSky,NightSun=_settings.NightSun,NightIntensity=_settings.NightIntensity,TransitionSpeed=_settings.DayNightSpeed };
                _release.Add(_lighting.Dispose);_lighting.BindStage(_owner,_bindings.VisionSource!=null?(Vector3?)_bindings.VisionSource.position:null);_lighting.SetVisionTest(On(SceneFeature.Vision));_lighting.SetNight(_settings.Night,true);
            }
            if(global && (On(SceneFeature.Pixels)||On(SceneFeature.VisionBlur))) {
                if(_bindings.Camera==null || _profile.Pipeline==null)Diagnostics.Add("화면 출력: 카메라와 URP 설정이 필요합니다.");
                else { _output=new CameraPresentation(root,_bindings.Camera,_profile.Pipeline);_output.Configure(On(SceneFeature.Pixels),On(SceneFeature.VisionBlur),_settings.PixelHeight);_release.Add(_output.Dispose); }
            }
            if(global && Need(SceneFeature.Bloom,_bindings.Camera!=null,"카메라가 필요합니다.")) { var bloom=new CrystalGlowEffect(root,_bindings.Camera,_bindings.BloomTargets);_release.Add(bloom.Dispose); }
            if(On(SceneFeature.Smoke)) foreach(var emitter in _bindings.SmokeEmitters) {
                if(emitter==null)continue;var anchor=new GameObject("SmokeEmitter").transform;anchor.SetParent(root,false);anchor.position=emitter.position;
                var smoke=new SmokeEffect(anchor,t=>t.position,_profile.SmokePrefab,_wind!=null?(Func<Vector3,Vector3>)_wind.Sample:null,isEmitter:t=>t==anchor);_smokes.Add(smoke);_release.Add(smoke.Dispose);
            }
        }
        private readonly List<SmokeEffect> _smokes=new List<SmokeEffect>();
        private void OnContact(FeedbackTarget target,Vector3 point)=>Contact?.Invoke(target,point);
        public void SetNight(bool night,bool immediate=false)=>_lighting?.SetNight(night,immediate);
        // Null returns control to the normal day/night transition.
        public void SetLightingBlend(float? blend)=>_lighting?.SetFilmBlend(blend.HasValue?Mathf.Clamp01(blend.Value):(float?)null);
        public void SetVision(bool enabled)
        {
            _settings.Set(SceneFeature.Vision,enabled);_settings.Set(SceneFeature.VisionBlur,enabled);
            _lighting?.SetVisionTest(enabled);
        }
        public void PresentImpact(FeedbackTarget target,Vector3 point,Vector3 normal,Color color)
        {
            if(target==null || !_targets.Contains(target))return;
            _impact?.PresentImpact(target,point,normal,color);
        }
        // Automatic work samples the same mesh triangle/material/vertex color as direct picking.
        public bool PresentSurfaceImpact(FeedbackTarget target,Vector3 preferredPoint)
        {
            return SurfaceImpact(target,preferredPoint,false,null);
        }
        public bool BeginSurfaceImpact(FeedbackTarget target,Vector3 preferredPoint,Action contacted)
        {
            return SurfaceImpact(target,preferredPoint,true,contacted);
        }
        private bool SurfaceImpact(FeedbackTarget target,Vector3 preferredPoint,bool swing,Action contacted)
        {
            if(target==null || !_targets.Contains(target) || _impact==null || _bindings.Camera==null)return false;
            var camera=_bindings.Camera.transform;
            var surfaces=target.Visual.GetComponentsInChildren<PickSurface>(true);
            var aims=new[]{preferredPoint,preferredPoint+camera.right*.16f,preferredPoint-camera.right*.16f,
                preferredPoint+Vector3.up*.22f,preferredPoint-Vector3.up*.22f};
            foreach(var aim in aims)
            {
                var direction=(aim-camera.position).normalized;
                var ray=new Ray(camera.position,direction);
                PickSurface nearest=null;float distance=float.PositiveInfinity;Color color=default;
                foreach(var surface in surfaces)
                    if(surface!=null && ReferenceEquals(surface.Target,target) && surface.Intersect(ray,out var hit,out var sampled) && hit<distance)
                    {nearest=surface;distance=hit;color=sampled;}
                if(nearest==null)continue;
                if(swing)return _impact.TryBeginImpact(target,nearest,ray.GetPoint(distance),color,contacted);
                _impact.PresentImpact(target,ray.GetPoint(distance),nearest.HitNormal,color);
                return true;
            }
            return false;
        }
        public void PresentGroundTap(Vector3 worldPoint)
        {
            if(Geometry==null || _impact==null)return;
            var local=_owner.InverseTransformPoint(worldPoint);
            var ground=_owner.TransformPoint(new Vector3(local.x,Geometry.Height(local.x,local.z),local.z));
            _impact.TapFeedback(ground,Vector3.up,Geometry.SurfaceColor(local.x,local.z));
        }
        public void Tick(float delta,bool paused=false)
        {
            if(_disposed)return;delta=paused?0:Mathf.Max(0,delta);_time+=delta;
            if(_paused!=paused) { foreach(var audio in GeneratedRoot.GetComponentsInChildren<AudioSource>(true)) { if(paused)audio.Pause();else audio.UnPause(); } _paused=paused; }
            _hover?.SetPaused(paused);foreach(var smoke in _smokes){smoke.SetPaused(paused);smoke.TickWind();}
            foreach(var target in _targets)if(target!=null)target.SetFeatureFlags(On(SceneFeature.HitFeedback),On(SceneFeature.DamageMarks),On(SceneFeature.Snow),On(SceneFeature.Destruction));
            if(paused)return;
            _wind?.Tick(_time,_settings.WindStrength);_lighting?.Tick(delta);
            if(_lighting!=null && _bindings.VisionSource!=null)_lighting.SetSource(_bindings.VisionSource.position,_settings.SourceRadius);
            _hover?.Tick(delta);_impact?.Tick(delta);foreach(var target in _targets)if(target!=null)target.Tick(delta);
            _impact?.AnimateTool(delta);_shake?.Tick(_impact!=null?_impact.Shake:0,_settings.HitShakeStrength);foreach(var leaf in _leaves)leaf.Tick(_bindings.Camera);_grass?.Tick(_bindings.Camera);
            _output?.ResizeIfNeeded(On(SceneFeature.Pixels),On(SceneFeature.VisionBlur),_settings.PixelHeight);_output?.SetVisionBlur(On(SceneFeature.VisionBlur),_lighting);
            _ambient?.Tick(delta,true,_lighting==null||!_lighting.Night,_brushing);
        }
        public bool ScreenRay(Vector2 point,out Ray ray)
        {
            ray=default;if(_bindings.Camera==null)return false;var rect=_output!=null?_output.Display:_bindings.Camera.pixelRect;
            if(!rect.Contains(point))return false;var uv=new Vector3((point.x-rect.x)/rect.width,(point.y-rect.y)/rect.height,0);
            var texture=_output?.Texture;if(texture!=null){uv.x=(Mathf.Floor(uv.x*texture.width)+.5f)/texture.width;uv.y=(Mathf.Floor(uv.y*texture.height)+.5f)/texture.height;}
            ray=_bindings.Camera.ViewportPointToRay(uv);return true;
        }
        public Vector2 WorldToScreenPoint(Vector3 point)
        {
            if(_bindings.Camera==null)return Vector2.zero;
            var viewport=_bindings.Camera.WorldToViewportPoint(point);
            var rect=_output!=null?_output.Display:_bindings.Camera.pixelRect;
            return new Vector2(rect.x+viewport.x*rect.width,rect.y+viewport.y*rect.height);
        }
        public bool Pointer(Ray? ray,bool moving,bool click,out FeedbackTarget target,out Vector3 point)
        {
            target=null;point=default;_brushing=false;if(_hovered!=null)_hovered.SetHover(false);_hovered=null;_hover?.SetPointer(null);_lighting?.UpdateCursor(ray);
            if(!ray.HasValue || !On(SceneFeature.Picking))return false;
            var r=ray.Value;bool objectHit=_picks.Pick(r,_bindings.Camera!=null?_bindings.Camera.farClipPlane:1000,out var surface,out var objectPoint,out var color);
            bool groundHit=false;Vector3 ground=default;
            if(Geometry!=null) { var localRay=new Ray(_owner.InverseTransformPoint(r.origin),_owner.InverseTransformVector(r.direction));groundHit=Geometry.Raycast(localRay,out var local,_hover==null?null:(Func<Vector3,float>)(v=>_hover.HeightOffset(_owner.TransformPoint(v))));ground=_owner.TransformPoint(local); }
            if(objectHit && (!groundHit || Vector3.Distance(r.origin,objectPoint)<=Vector3.Distance(r.origin,ground))) {
                point=objectPoint;target=surface.Target as FeedbackTarget;_hovered=target;target?.SetHover(true);
                if(click) { if(target!=null)_impact?.BeginImpact(target,surface,point,color);else _impact?.TapFeedback(point,surface.HitNormal,color); }
                return true;
            }
            if(!groundHit)return false;point=ground;var lp=_owner.InverseTransformPoint(ground);var biome=Map.At(Mathf.FloorToInt(lp.x+.5f),Mathf.FloorToInt(lp.z+.5f));
            _hover?.SetPointer(ground,biome,moving);_brushing=moving&&_grass!=null&&_grass.HasGrassAt(ground);
            if(click)_impact?.TapFeedback(ground,Vector3.up,Geometry.SurfaceColor(lp.x,lp.z));return true;
        }
        public void Reset()
        {
            _time=0;_impact?.Reset();_shake?.Tick(0,0);_hover?.Reset();_lighting?.ResetVisionTrail();_sound?.Reset();_ambient?.Reset();foreach(var smoke in _smokes)smoke.ResetTake();foreach(var target in _targets)if(target!=null)target.ResetTake();
        }
        public void Dispose()
        {
            if(_disposed)return;_disposed=true;
            for(int i=_release.Count-1;i>=0;i--)try{_release[i]();}catch(Exception error){Debug.LogException(error);}
            _release.Clear();_picks.Clear();if(_activeEnvironment==this)_activeEnvironment=null;
            if(GeneratedRoot!=null){GeneratedRoot.SetActive(false);Release(GeneratedRoot);GeneratedRoot=null;}
        }
        public static void Release(UnityEngine.Object value) { if(value==null)return;if(Application.isPlaying)Afterlife.Modules.UnitySupport.ObjectLifetime.Release(value);else UnityEngine.Object.DestroyImmediate(value); }
    }
}
