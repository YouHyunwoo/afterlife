using UnityEngine;
using System.Collections.Generic;
using Afterlife.Devlog.Terrain;
using Afterlife.Devlog.Destruction;
using Afterlife.Devlog.Contracts;

namespace Afterlife.Devlog.Day01
{
    // 촬영용 반응만 담당한다. 게임 자원/전투 상태를 참조하거나 변경하지 않는다.
    public sealed class ShowcaseTarget : MonoBehaviour, ISnowTarget
    {
        public Transform Visual;
        Transform ISnowTarget.Visual => Visual;
        public bool Juicy;
        public bool Wood;
        public DestructionProfile DestructionSettings;
        public ShowcaseBiome Biome { get; private set; }
        public bool HasSnow => _biomeStage != null && _modelStage && Biome == ShowcaseBiome.Snow;
        private Transform _biomeStage;
        private bool _modelStage;
        private Renderer[] _renderers;
        private MaterialPropertyBlock _properties;
        private Vector3 _restScale;
        private Quaternion _restRotation;
        private Vector3 _restPosition, _recoilTranslation, _recoilAxis;
        private float _age = 10;
        private bool _hover;
        private readonly Vector4[] _snowHits = new Vector4[32];
        private int _snowHitCount;
        private readonly List<ShowcaseSnowCap> _snowCaps = new List<ShowcaseSnowCap>();
        private bool _initialized;
        private int _hits;
        public bool IsBroken { get; private set; }
        public float FallSoundDuration { get; set; } = 2.94f;
        public DisappearMode RemovalMode { get; set; }
        private ShowcaseDestruction _destruction;

        private float GroundHeight(Vector3 world)
        {
            if (_biomeStage==null) return transform.position.y;
            Vector3 p=_biomeStage.InverseTransformPoint(world);
            int x=Mathf.FloorToInt(p.x),z=Mathf.FloorToInt(p.z);
            var grid=_biomeStage.Find("Terrain_Flat/BiomeGrid_12x10");
            if (grid!=null)
                foreach (Transform tile in grid)
                {
                    if (Mathf.FloorToInt(tile.localPosition.x)!=x || Mathf.FloorToInt(tile.localPosition.z)!=z) continue;
                    var filter=tile.GetComponent<MeshFilter>();
                    if (filter!=null && filter.sharedMesh!=null && filter.sharedMesh.name.EndsWith("_Flat"))
                        return _biomeStage.position.y;
                }
            return _biomeStage.position.y+(float)ShowcaseTileVariants.Height(x,z,p.x-x,p.z-z);
        }
        public static bool IsPineCrown(Renderer renderer) => renderer != null && renderer.name.StartsWith("Tree1_Mesh");
        public float SnowRemaining(Vector3 world)
        {
            Vector3 local = Visual.InverseTransformPoint(world);
            float remaining = 1;
            for (int i = 0; i < _snowHitCount; i++)
            {
                remaining *= 1 - _snowHits[i].w * ShowcaseSnowErosion.Influence(local,(Vector3)_snowHits[i]);
            }
            return remaining;
        }
        public bool SnowVisible(Vector3 world) => ShowcaseSnowErosion.Visible(Visual.InverseTransformPoint(world),SnowRemaining(world));

        public List<Vector3> ShedSnow(Vector3 world)
        {
            var particles=new List<Vector3>();
            if (!HasSnow) return particles;
            var samples=new List<ShowcaseSnowCap.Sample>();
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
            var candidates=new List<ShowcaseSnowCap.Sample>();
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
            ApplyProperties(.72f);
            return particles;
        }
        public bool CanHit => !IsBroken && _age >= .24f;

        private void Awake() { Initialize(); }

        public void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            if (Visual == null) Visual = transform;
            _restScale = Visual.localScale;
            _restRotation = Visual.localRotation;
            _restPosition = Visual.localPosition;
            _renderers = Visual.GetComponentsInChildren<Renderer>();
            _properties = new MaterialPropertyBlock();
            var showcase = GetComponentInParent<Day01Showcase>();
            if (showcase != null && showcase.Stages != null)
                for (int i = 0; i < showcase.Stages.Length; i++)
                    if (showcase.Stages[i] != null && transform.IsChildOf(showcase.Stages[i]))
                    { _biomeStage = showcase.Stages[i]; _modelStage = i > 0; break; }
            RefreshBiome();
            if (_modelStage)
                foreach (var surface in Visual.GetComponentsInChildren<ShowcasePickSurface>(true))
                    if (surface.Target == this && !surface.SnowCap) _snowCaps.Add(new ShowcaseSnowCap(surface,this,IsPineCrown(surface.VisibleRenderer)));
            _renderers = Visual.GetComponentsInChildren<Renderer>();
            ApplyProperties(0);
        }

        private void RefreshBiome()
        {
            if (_biomeStage == null) return;
            Vector3 position = _biomeStage.InverseTransformPoint(transform.position);
            var biome = ShowcaseBiomeMap.At(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.z));
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
            // 완성 단계에서 선택한 파괴음을 비교하기 위한 촬영용 돌 파괴.
            if (Juicy && ++_hits>=(DestructionSettings!=null ? Mathf.Max(1,DestructionSettings.HitsToBreak) : 4))
            {
                IsBroken=true;
                _hover=false;
                _destruction=new ShowcaseDestruction(Visual,_renderers,Wood,RemovalMode,GroundHeight,away,DestructionSettings);
                if (!Wood) foreach (var renderer in _renderers) renderer.enabled=false;
            }
        }

        public void ResetTake()
        {
            _destruction?.Dispose(); _destruction=null;
            if (IsBroken && _renderers!=null)
                foreach (var renderer in _renderers) renderer.enabled=true;
            IsBroken=false;
            _hits=0;
            _age = 10;
            _hover = false;
            _snowHitCount = 0;
            System.Array.Clear(_snowHits,0,_snowHits.Length);
            if (Visual == null || _properties == null) return;
            Visual.localScale = _restScale;
            Visual.localPosition = _restPosition;
            Visual.localRotation = _restRotation;
            foreach (var cap in _snowCaps) cap.Refresh();
            ApplyProperties(0);
        }

        private void Update()
        {
            if (IsBroken)
            {
                if (Wood)
                {
                    _age+=Time.unscaledDeltaTime;
                    float fall=Mathf.Clamp01(Mathf.Max(0,_age-ShowcaseImpactTiming.Hold)/(DestructionSettings!=null ? Mathf.Max(.1f,DestructionSettings.TreeFallSeconds) : 1.1f));
                    float degrees=DestructionSettings!=null ? DestructionSettings.TreeFallDegrees : 88;
                    Visual.localRotation=Quaternion.AngleAxis(degrees*fall*fall,_recoilAxis)*_restRotation;
                    ApplyProperties(ShowcaseImpactTiming.Flash(_age));
                }
                _destruction?.Tick(Time.unscaledDeltaTime);
                return;
            }
            RefreshBiome();
            _age += Time.unscaledDeltaTime;
            float pulse = Juicy ? ShowcaseImpactTiming.Recoil(_age,Wood) : 0;
            Visual.localScale = _restScale;
            Visual.localPosition = _restPosition + _recoilTranslation*pulse;
            Visual.localRotation = Quaternion.AngleAxis(pulse*(Wood ? 9 : 4),_recoilAxis.sqrMagnitude>.001f ? _recoilAxis : Vector3.right)*_restRotation;
            ApplyProperties(Juicy ? ShowcaseImpactTiming.Flash(_age) : Mathf.Clamp01(1-_age/.07f)*.72f);
        }

        private void ApplyProperties(float flash)
        {
            foreach (var renderer in _renderers)
            {
                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat("_Flash", flash);
                _properties.SetFloat("_Hover", _hover ? 1 : 0);
                bool cap = renderer.name == "SnowCap";
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

        private void OnDisable() { ResetTake(); }
        private void OnDestroy() { _destruction?.Dispose(); _destruction=null; foreach (var cap in _snowCaps) cap.Dispose(); _snowCaps.Clear(); }
    }
}
