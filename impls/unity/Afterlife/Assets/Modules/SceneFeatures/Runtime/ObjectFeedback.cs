using Afterlife.Modules.Snow;
using Afterlife.Modules.Interaction;
using UnityEngine;
using System.Collections.Generic;
using Afterlife.Modules.Terrain;
using Afterlife.Modules.Destruction;
using Afterlife.Modules.TargetContracts;

namespace Afterlife.Modules.SceneFeatures
{
    // 촬영용 반응만 담당한다. 게임 자원/전투 상태를 참조하거나 변경하지 않는다.
    public sealed class ObjectFeedback : System.IDisposable, ISnowTarget, IImpactReceiver
    {
        public bool IsWood=>Wood;
        public bool UseJuice=>Juicy;
        public Transform Root=>Owner;
        public bool Active=>Owner!=null&&Owner.gameObject.activeInHierarchy;
        public Transform Visual;
        public readonly Transform Owner;
        private readonly ISnowTarget _identity;
        public ObjectFeedback(Transform owner,Transform visual,ISnowTarget identity=null) { Owner=owner;Visual=visual;_identity=identity??this; }
        Transform ISnowTarget.Visual => Visual;
        public bool Juicy;
        public bool Wood;
        public bool EnableSnow = true;
        public DestructionProfile DestructionSettings;
        public TerrainBiome Biome { get; private set; }
        public bool HasSnow => EnableSnow && Biome == TerrainBiome.Snow;
        public System.Func<Vector3,float> GroundHeightQuery;
        public System.Func<Vector3,TerrainBiome> BiomeQuery;
        public Renderer[] PineCrowns = new Renderer[0];
        public bool HitFeedbackEnabled=true, DestructionEnabled=true;
        public event System.Action<Vector3> Contact;
        public System.Func<bool> CanReceiveHit;
        private void OnHitAccepted(Vector3 point) { Contact?.Invoke(point); }
        private Renderer[] _renderers;
        private MaterialPropertyBlock _properties;
        private Vector3 _restScale;
        private Quaternion _restRotation;
        private Vector3 _restPosition, _recoilTranslation, _recoilAxis;
        private float _age = 10;
        private bool _hover;
        private bool _propertiesDirty=true;
        private float _lastFlash=-1;
        private bool _lastHover,_lastSnow,_lastMarks,_lastJuicy,_lastWood;
        private Matrix4x4 _lastVisualMatrix;
        public int RenderVersion { get; private set; }
        private readonly Vector4[] _snowHits = new Vector4[32];
        private int _snowHitCount;
        private readonly List<SnowCapRenderer> _snowCaps = new List<SnowCapRenderer>();
        private bool _initialized,_disposed;
        private readonly Afterlife.Modules.DamageMarks.DamageMarkRenderer _damageMarks=new Afterlife.Modules.DamageMarks.DamageMarkRenderer();
        public bool DamageMarksEnabled { get; set; }=true;
        public void RecordDamageMark(Vector3 point,Vector3 normal,Color surfaceColor)
        {
            if (!Juicy || !CanHit) return;
            if (Wood && surfaceColor.g>surfaceColor.r*1.08f && surfaceColor.g>surfaceColor.b*1.05f) return;
            _damageMarks.Add(Visual,point,normal);
            _propertiesDirty=true;
        }
        public bool IsBroken { get; private set; }
        public float FallSoundDuration { get; set; } = 2.94f;
        public DisappearMode RemovalMode { get; set; }
        private DestructionEffect _destruction;

        private float GroundHeight(Vector3 world) => GroundHeightQuery != null ? GroundHeightQuery(world) : Owner.position.y;
        public bool IsPineCrown(Renderer renderer) => System.Array.IndexOf(PineCrowns,renderer)>=0;
        public float SnowRemaining(Vector3 world)
        {
            Vector3 local = Visual.InverseTransformPoint(world);
            float remaining = 1;
            for (int i = 0; i < _snowHitCount; i++)
            {
                remaining *= 1 - _snowHits[i].w * SnowErosion.Influence(local,(Vector3)_snowHits[i]);
            }
            return remaining;
        }
        public bool SnowVisible(Vector3 world) => SnowErosion.Visible(Visual.InverseTransformPoint(world),SnowRemaining(world));

        public List<Vector3> ShedSnow(Vector3 world)
        {
            var particles=new List<Vector3>();
            if (!HasSnow) return particles;
            var samples=new List<SnowCapRenderer.Sample>();
            foreach (var cap in _snowCaps) cap.CollectSamples(world,samples);
            Vector3 local = Visual.InverseTransformPoint(world);
            int slot = -1;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < _snowHitCount; i++)
            {
                float distance = Vector3.Distance(local,(Vector3)_snowHits[i]);
                if (distance < nearest) { nearest = distance; slot = i; }
            }
            if (nearest > .12f && _snowHitCount < _snowHits.Length)
            { slot = _snowHitCount++; _snowHits[slot] = new Vector4(local.x,local.y,local.z,0); }
            if (slot >= 0) _snowHits[slot].w = Mathf.Min(1,_snowHits[slot].w+.55f);
            var candidates=new List<SnowCapRenderer.Sample>();
            float total=0;
            foreach (var sample in samples)
            {
                float lost=sample.Before-SnowRemaining(sample.MaskPosition);
                if (lost<=.02f) continue;
                var changed=sample; changed.Weight*=lost;
                candidates.Add(changed); total+=changed.Weight;
            }
            var random=new System.Random(781+_snowHitCount*37);
            while (particles.Count<20 && candidates.Count>0 && total>0)
            {
                float choice=(float)random.NextDouble()*total;
                int selected=candidates.Count-1;
                for (int i=0;i<candidates.Count;i++)
                { choice-=candidates[i].Weight; if (choice<=0) { selected=i; break; } }
                particles.Add(candidates[selected].Position);
                total-=candidates[selected].Weight; candidates.RemoveAt(selected);
            }
            foreach (var cap in _snowCaps) cap.Refresh();
            _propertiesDirty=true;
            ApplyProperties(.72f);
            return particles;
        }
        public void SetFeatureFlags(bool hit,bool marks,bool snow,bool destruction)
        {
            bool changed=EnableSnow!=snow;
            HitFeedbackEnabled=hit;DamageMarksEnabled=marks;EnableSnow=snow;DestructionEnabled=destruction;
            if(snow && _snowCaps.Count==0 && Visual!=null) {
                foreach(var surface in Visual.GetComponentsInChildren<PickSurface>(true))if(ReferenceEquals(surface.Target,_identity)&&!surface.SnowCap)_snowCaps.Add(new SnowCapRenderer(surface,_identity,IsPineCrown(surface.VisibleRenderer)));
                _renderers=Visual.GetComponentsInChildren<Renderer>();
            }
            if(changed) { foreach(var cap in _snowCaps)cap.Refresh();_propertiesDirty=true; }
        }
        public bool CanHit => !IsBroken && _age >= .24f && (CanReceiveHit==null || CanReceiveHit());


        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (Visual == null) Visual = Owner;
            _restScale = Visual.localScale;
            _restRotation = Visual.localRotation;
            _restPosition = Visual.localPosition;
            _renderers = Visual.GetComponentsInChildren<Renderer>();
            _properties = new MaterialPropertyBlock();
            RefreshBiome();
            if (EnableSnow)
                foreach (var surface in Visual.GetComponentsInChildren<PickSurface>(true))
                    if (ReferenceEquals(surface.Target, _identity) && !surface.SnowCap) _snowCaps.Add(new SnowCapRenderer(surface,_identity,IsPineCrown(surface.VisibleRenderer)));
            _renderers = Visual.GetComponentsInChildren<Renderer>();
            ApplyProperties(0);
        }

        private void RefreshBiome()
        {
            if (BiomeQuery == null) return;
            var biome = BiomeQuery(Owner.position);
            if (biome == Biome) return;
            Biome = biome;
            foreach (var cap in _snowCaps) cap.Refresh();
        }

        public void SetHover(bool value) { _hover = value; }
        public void Hit(Vector3 hitPoint, Vector3 incomingDirection)
        {
            if (!CanHit) return;
            // 밑동 중심을 기준으로 맞은 쪽의 반대 방향을 구한다. 지면 아래로 밀리지 않는다.
            Vector3 center = Visual.parent != null ? Visual.parent.TransformPoint(_restPosition) : _restPosition;
            Vector3 away = Vector3.ProjectOnPlane(center-hitPoint,Vector3.up);
            if (away.sqrMagnitude < .0025f) away = Vector3.ProjectOnPlane(incomingDirection,Vector3.up);
            if (away.sqrMagnitude < .0001f) away = Vector3.forward;
            away.Normalize();
            Vector3 worldAxis = Vector3.Cross(Vector3.up,away).normalized;
            _recoilAxis = Visual.parent != null ? Visual.parent.InverseTransformDirection(worldAxis) : worldAxis;
            Vector3 displacement = away*(Wood ? .04f : .085f);
            _recoilTranslation = Visual.parent != null ? Visual.parent.InverseTransformVector(displacement) : displacement;
            _age = 0;
            ApplyProperties(.72f);
            OnHitAccepted(hitPoint);
        }
        public void Break(Vector3 away)
        {
            if(IsBroken || !DestructionEnabled) return;
            IsBroken=true; _hover=false; _age=0;
            _destruction=new DestructionEffect(Visual,_renderers,Wood,RemovalMode,GroundHeight,away,DestructionSettings);
            if(!Wood) foreach(var renderer in _renderers) if(renderer!=null)renderer.enabled=false;
        }

        public void ResetTake()
        {
            if(_disposed)return;
            _destruction?.Dispose(); _destruction=null;
            if (IsBroken && _renderers!=null)
                foreach (var renderer in _renderers) if(renderer!=null)renderer.enabled=true;
            IsBroken=false;
            _damageMarks.Clear();
            _age = 10;
            _hover = false;
            _snowHitCount = 0;
            _propertiesDirty=true;
            System.Array.Clear(_snowHits,0,_snowHits.Length);
            if (Visual == null || _properties == null) return;
            Visual.localScale = _restScale;
            Visual.localPosition = _restPosition;
            Visual.localRotation = _restRotation;
            foreach (var cap in _snowCaps) cap.Refresh();
            ApplyProperties(0);
        }

        public void Tick(float delta)
        {
            if (IsBroken)
            {
                if (_destruction!=null && _destruction.Completed) return;
                if (Wood)
                {
                    _age+=delta;
                    float fall=Mathf.Clamp01(Mathf.Max(0,_age-ImpactTiming.Hold)/(DestructionSettings!=null ? Mathf.Max(.1f,DestructionSettings.TreeFallSeconds) : 1.1f));
                    float degrees=DestructionSettings!=null ? DestructionSettings.TreeFallDegrees : 88;
                    Visual.localRotation=Quaternion.AngleAxis(degrees*fall*fall,_recoilAxis)*_restRotation;
                    ApplyProperties(ImpactTiming.Flash(_age));
                }
                _destruction?.Tick(delta);
                RenderVersion++;
                return;
            }
            RefreshBiome();
            if (_age>=1.25f) { ApplyProperties(0); return; }
            _age += delta;
            float pulse = HitFeedbackEnabled && Juicy && _age<1.25f ? ImpactTiming.Recoil(_age,Wood) : 0;
            Visual.localScale = _restScale;
            Visual.localPosition = _restPosition + _recoilTranslation*pulse;
            Visual.localRotation = Quaternion.AngleAxis(pulse*(Wood ? 9 : 4),_recoilAxis.sqrMagnitude>.001f ? _recoilAxis : Vector3.right)*_restRotation;
            ApplyProperties(Juicy ? ImpactTiming.Flash(_age) : Mathf.Clamp01(1-_age/.07f)*.72f);
        }

        private void ApplyProperties(float flash)
        {
            if(_disposed || Visual==null || _properties==null || _renderers==null)return;
            if(!HitFeedbackEnabled) flash=0;
            var matrix=Visual.worldToLocalMatrix;
            if (!_propertiesDirty && _lastFlash==flash && _lastHover==_hover && _lastSnow==HasSnow
                && _lastMarks==DamageMarksEnabled && _lastJuicy==Juicy && _lastWood==Wood && _lastVisualMatrix==matrix) return;
            _propertiesDirty=false; _lastFlash=flash; _lastHover=_hover; _lastSnow=HasSnow;
            _lastMarks=DamageMarksEnabled; _lastJuicy=Juicy; _lastWood=Wood; _lastVisualMatrix=matrix;
            RenderVersion++;
            foreach (var renderer in _renderers)
            {
                if(renderer==null)continue;
                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat("_Flash", flash);
                _properties.SetFloat("_Hover", _hover ? 1 : 0);
                bool cap = renderer.name == "SnowCap";
                _damageMarks.Write(_properties,Visual,Juicy && DamageMarksEnabled && !cap,Wood);
                _properties.SetFloat("_Snow", cap && HasSnow ? 1 : 0);
                _properties.SetFloat("_LeafSnow",HasSnow ? 1 : 0);
                _properties.SetFloat("_LeafSnowPine",IsPineCrown(renderer) ? 1 : 0);
                _properties.SetFloat("_SnowShell", cap ? 1 : 0);
                _properties.SetFloat("_SnowPine", cap ? 1 : 0);
                _properties.SetInt("_SnowHitCount", _snowHitCount);
                _properties.SetVectorArray("_SnowHits", _snowHits);
                _properties.SetMatrix("_SnowWorldToLocal", Visual.worldToLocalMatrix);
                renderer.SetPropertyBlock(_properties);
            }
        }

        public void Dispose()
        {
            if(_disposed)return;
            ResetTake();_disposed=true;
            foreach(var cap in _snowCaps)cap.Dispose();
            _snowCaps.Clear();_renderers=System.Array.Empty<Renderer>();
            Contact=null;CanReceiveHit=null;
        }
    }
}
