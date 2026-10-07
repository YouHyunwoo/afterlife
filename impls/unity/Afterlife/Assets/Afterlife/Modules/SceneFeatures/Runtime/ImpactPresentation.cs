using Afterlife.Modules.Interaction;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Modules.SceneFeatures
{
    // Owns tool poses and pooled hit debris; the showcase only chooses a hit surface.
    public class ImpactPresentation : IDisposable
    {
        private const int MaximumChips = 256;
        public float ImpactTime = .12f;
        private const float ContactHold = Afterlife.Modules.Destruction.ImpactTiming.Hold;
        public float SwingDuration = .285f;
        private readonly Transform _owner;
        private readonly Camera _camera;
        private readonly GameObject _axe;
        private readonly GameObject _pick;
        private readonly GameObject _chipPrefab;
        private readonly Material _particleMaterial;
        private readonly Afterlife.Modules.WorldAudio.WorldAudioPlayer _sound;
        private readonly Transform _axeImpactPoint;
        public bool ToolsEnabled=true, ClickParticlesEnabled=true, HitParticlesEnabled=true, SoundEnabled=true;
        private float _time;
        private readonly Transform _pickImpactPoint;
        private readonly MaterialPropertyBlock _properties = new MaterialPropertyBlock();
        private readonly List<Chip> _chips = new List<Chip>();
        private Afterlife.Modules.Destruction.ImpactChipPool _chipPool;
        private IImpactReceiver _pendingTarget;
        private Action _contacted;
        private GameObject _activeTool;
        private Transform _activeImpactPoint;
        private Transform _hitSurface;
        private Vector3 _hitLocalPoint;
        private Vector3 _toolPoint;
        private Vector3 _approach;
        private Vector3 _impactNormal = Vector3.up;
        private Color _impactColor = Color.white;
        private Quaternion _swingBasis;
        private float _toolAge = 10;
        private float _shake;
        private readonly ToolContactClock _clock=new ToolContactClock();

        private struct Chip
        {
            public Transform Transform;
            public Vector3 Velocity;
            public float Age;
            public bool Snow, Tap;
            public float FloorY;
        }

        public bool IsSwinging => _pendingTarget != null;
        public float Shake => _shake;

        public ImpactPresentation(Transform owner, Camera camera, GameObject axe, GameObject pick,
            GameObject chipPrefab, Material particleMaterial, Afterlife.Modules.WorldAudio.WorldAudioPlayer sound, Transform axeContact=null, Transform pickContact=null)
        {
            _owner = owner;
            _camera = camera;
            _axe = axe;
            _pick = pick;
            _chipPrefab = chipPrefab;
            _particleMaterial = particleMaterial;
            _sound = sound;
            _axeImpactPoint = axeContact;
            _pickImpactPoint = pickContact;
        }

        public void BeginImpact(IImpactReceiver target, PickSurface surface, Vector3 point, Color color)
        {
            TryBeginImpact(target, surface, point, color, null);
        }

        // An external model can synchronize its accepted-work presentation with the same Day01 swing.
        public bool TryBeginImpact(IImpactReceiver target, PickSurface surface, Vector3 point, Color color, Action contacted)
        {
            if (target == null || !target.CanHit || IsSwinging || surface == null) return false;
            _toolPoint = point;
            _impactColor = color;
            _impactNormal = surface.HitNormal.normalized;
            if (_activeTool != null) _activeTool.SetActive(false);
            _activeTool = target.IsWood ? _axe : _pick;
            _activeImpactPoint = target.IsWood ? _axeImpactPoint : _pickImpactPoint;
            if (!ToolsEnabled || !target.UseJuice || _activeTool == null || _activeImpactPoint == null)
            {
                ApplyImpact(target, point);
                contacted?.Invoke();
                return true;
            }
            _pendingTarget = target;
            _contacted = contacted;
            _hitSurface = surface.transform;
            _hitLocalPoint = _hitSurface.InverseTransformPoint(point);
            _swingBasis = _camera.transform.rotation;
            _approach = _camera.transform.right * .45f + _camera.transform.up * .55f - _camera.transform.forward * .25f;
            _toolAge = 0;
            _clock.Begin();
            _activeTool.SetActive(true);
            AnimateTool(0);
            return true;
        }

        // External simulation already accepted this work; play its surface feedback immediately.
        public void PresentImpact(IImpactReceiver target, Vector3 point, Vector3 normal, Color color)
        {
            if(target==null || !target.CanHit)return;
            _impactNormal=normal.normalized;_impactColor=color;ApplyImpact(target,point);
        }

        public void TapFeedback(Vector3 point, Vector3 normal, Color color)
        {
            if (!ClickParticlesEnabled) return;
            Vector3 tangent = Vector3.Cross(normal, Mathf.Abs(normal.y) > .9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);
            for (int i = 0; i < 4; i++)
            {
                var go = CreateChip(); go.name = "ClickChip";
                go.transform.SetParent(_owner, false); go.transform.position = point + normal * .012f;
                go.transform.localScale = Vector3.one * .075f;
                var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = _particleMaterial;
                _properties.Clear(); _properties.SetVector("_BaseColor", (Vector4)color); renderer.SetPropertyBlock(_properties);
                float angle = i * 2.39996f;
                _chips.Add(new Chip { Transform = go.transform, Tap = true, Velocity = normal * (.7f + i * .05f) + Vector3.up * .5f
                    + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * .48f });
            }
        }

        public void Tick(float deltaTime)
        {
            _time += deltaTime;
            _shake = Mathf.Max(0, _shake - deltaTime);
            for (int i = _chips.Count - 1; i >= 0; i--)
            {
                var chip = _chips[i];
                chip.Age += deltaTime;
                float lifetime = chip.Tap ? .5f : chip.Snow ? 1.05f : .65f;
                if (chip.Age > lifetime) { _chipPool.Return(chip.Transform.gameObject); _chips.RemoveAt(i); continue; }
                chip.Velocity += Vector3.down * (chip.Tap ? 3 : chip.Snow ? 7.5f : 9) * deltaTime;
                chip.Transform.position += chip.Velocity * deltaTime;
                if (chip.Snow && chip.Transform.position.y < chip.FloorY + .012f)
                {
                    Vector3 position = chip.Transform.position; position.y = chip.FloorY + .012f;
                    chip.Transform.position = position; chip.Velocity = Vector3.zero;
                    chip.Age = Mathf.Max(chip.Age, lifetime - .16f);
                }
                chip.Transform.Rotate(new Vector3(110, 160, 70) * deltaTime);
                chip.Transform.localScale = (chip.Tap ? Vector3.one * .075f : chip.Snow ? new Vector3(.045f,.018f,.045f) : Vector3.one * .075f)
                    * Mathf.Clamp01((lifetime - chip.Age) / .2f);
                _chips[i] = chip;
            }
        }

        public void AnimateTool(float deltaTime)
        {
            if (_pendingTarget == null || !_pendingTarget.Active || _activeTool == null || _activeImpactPoint == null || _hitSurface == null)
            {
                if (_activeTool != null) _activeTool.SetActive(false);
                _pendingTarget = null;
                _contacted = null;
                return;
            }
            bool contactNow=_clock.Tick(deltaTime,ImpactTime);
            _toolAge=_clock.Age;
            if (_toolAge >= SwingDuration)
            { _activeTool.SetActive(false); _pendingTarget = null; _contacted = null; return; }
            _toolPoint = _hitSurface.TransformPoint(_hitLocalPoint);
            float approach = Mathf.Clamp01(_toolAge / ImpactTime);
            approach *= approach;
            float recovery = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(ImpactTime + ContactHold, SwingDuration, _toolAge));
            Vector3 contactPosition = _toolPoint + _approach * (1 - approach + recovery * .65f);
            float angle = Mathf.Lerp(-65, 25, approach) - recovery * 35;
            _activeTool.transform.rotation = _swingBasis * Quaternion.Euler(0, 0, angle);
            _activeTool.transform.position += contactPosition - _activeImpactPoint.position;
            if (contactNow)
            {
                ApplyImpact(_pendingTarget, _toolPoint);
                var contacted = _contacted; _contacted = null;
                contacted?.Invoke();
            }
        }

        public void Reset()
        {
            foreach (var chip in _chips)
                if (chip.Transform != null) _chipPool?.Return(chip.Transform.gameObject);
            _chips.Clear();
            if (_axe != null) _axe.SetActive(false);
            if (_pick != null) _pick.SetActive(false);
            _shake = 0; _toolAge = 10;
            _pendingTarget = null; _clock.Reset();
            _contacted = null;
            _hitSurface = null; _activeImpactPoint = null;
            _impactColor = Color.white; _impactNormal = Vector3.up;
        }

        public void Dispose()
        {
            Reset();
            _chipPool?.Dispose();
            _chipPool = null;
        }

        private GameObject CreateChip()
        {
            if (_chipPool == null) _chipPool = new Afterlife.Modules.Destruction.ImpactChipPool(_owner, _chipPrefab);
            if (_chips.Count >= MaximumChips) { _chipPool.Return(_chips[0].Transform.gameObject); _chips.RemoveAt(0); }
            return _chipPool.Rent();
        }

        private void ApplyImpact(IImpactReceiver target, Vector3 hitPoint)
        {
            if (!target.CanHit) return;
            Vector3 incoming = _pendingTarget == target ? -_approach : _camera.transform.forward;
            var snowPositions = target.ShedSnow(hitPoint);
            target.RecordDamageMark(hitPoint, _impactNormal, _impactColor);
            target.Hit(hitPoint, incoming);
            if(SoundEnabled) _sound?.Play(target.IsWood, target.IsBroken, _time);
            if (!target.UseJuice || !HitParticlesEnabled) return;
            _shake = .14f;
            int debrisCount = target.IsBroken ? 24 : 9;
            Vector3 outward = _impactNormal.sqrMagnitude > .01f ? _impactNormal : -incoming.normalized;
            Vector3 tangent = Vector3.Cross(outward, Mathf.Abs(outward.y) > .9f ? Vector3.right : Vector3.up).normalized;
            Vector3 bitangent = Vector3.Cross(outward, tangent);
            for (int i = 0; i < debrisCount + snowPositions.Count; i++)
            {
                bool snow = i >= debrisCount;
                var go = CreateChip();
                go.name = snow ? "SnowChip" : "HitChip";
                go.transform.SetParent(_owner, false);
                go.transform.position = snow ? snowPositions[i - debrisCount] : hitPoint + outward * .012f;
                go.transform.localScale = snow ? new Vector3(.045f,.018f,.045f) : Vector3.one * .075f;
                var renderer = go.GetComponent<Renderer>(); renderer.sharedMaterial = _particleMaterial;
                _properties.Clear();
                _properties.SetVector("_BaseColor", snow ? new Vector4(.87f,.94f,1,1) : (Vector4)_impactColor);
                renderer.SetPropertyBlock(_properties);
                float angle = i * 2.39996f;
                _chips.Add(new Chip { Transform = go.transform, Snow = snow, FloorY = target.Root.position.y, Velocity = snow
                    ? new Vector3(Mathf.Cos(angle) * .40f, -.6f - ((i - debrisCount) % 7) * .12f, Mathf.Sin(angle) * .40f)
                    : outward * (1.2f + (i % 3) * .18f) + Vector3.up * .85f
                        + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * (.5f + (i % 4) * .13f) });
            }
        }

    }
}
