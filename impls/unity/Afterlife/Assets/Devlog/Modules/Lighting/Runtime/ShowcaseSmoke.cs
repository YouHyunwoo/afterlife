using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Afterlife.Devlog.Day01
{
    // Unity Billboard 렌더러가 각 파티클을 카메라를 향하는 quad로 생성한다.
    public sealed class ShowcaseSmoke : IDisposable
    {
        private readonly List<ParticleSystem> _systems = new List<ParticleSystem>();
        private readonly List<Material> _materials = new List<Material>();
        private readonly Func<Vector3,Vector3> _wind;

        public ShowcaseSmoke(Transform root, Func<Transform,Vector3?> chimneyPosition, ParticleSystem template=null, Func<Vector3,Vector3> wind=null)
        {
            _wind=wind;
            var shader = Shader.Find("Afterlife/Devlog/ShowcaseSmoke");
            if (shader == null) { Debug.LogError("Devlog smoke shader is missing."); return; }
            foreach (var house in root.GetComponentsInChildren<Transform>(true))
            {
                if (house.name != "House_Pivot") continue;
                Vector3? outlet = chimneyPosition(house);
                if (!outlet.HasValue) { Debug.LogError("Devlog: chimney mesh position unavailable.",house); continue; }
                var go = template!=null ? UnityEngine.Object.Instantiate(template.gameObject) : new GameObject("ChimneySmoke");
                go.name="ChimneySmoke";
                go.transform.SetParent(house,false);
                // FBX 변환을 마친 실제 굴뚝 입구의 월드 좌표를 사용한다.
                go.transform.position = outlet.Value + Vector3.up*.012f;
                go.transform.rotation = Quaternion.LookRotation(Vector3.up);
                var particles = go.GetComponent<ParticleSystem>();
                if (particles==null) { particles=go.AddComponent<ParticleSystem>(); ConfigureTemplate(particles); }
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var material = new Material(shader) { name = "Devlog Chimney Smoke" };
                material.SetFloat("_EmitterY",go.transform.position.y);
                _materials.Add(material);
                var renderer = go.GetComponent<ParticleSystemRenderer>();
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.sharedMaterial = material;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sortMode = ParticleSystemSortMode.Distance;
                _systems.Add(particles);
                if (_wind!=null)
                {
                    var noise=particles.noise;
                    noise.strength=.045f;
                }
                particles.Play();
            }
        }

        // Editor도 같은 기본값을 사용해 프리팹을 만든다. 이후 Inspector 편집을 런타임이 보존한다.
        public static void ConfigureTemplate(ParticleSystem particles)
        {
                particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.playOnAwake = false; main.loop = true; main.duration = 4;
                main.startLifetime = new ParticleSystem.MinMaxCurve(3.2f,4.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(.38f,.55f);
                main.startSize = new ParticleSystem.MinMaxCurve(.20f,.32f);
                main.startRotation = new ParticleSystem.MinMaxCurve(0,Mathf.PI*2);
                main.startColor = new Color(.72f,.75f,.79f,.35f);
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Shape;
                main.maxParticles = 64;
                var emission = particles.emission; emission.rateOverTime = 8;
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 9; shape.radius = .045f;
                var velocity = particles.velocityOverLifetime; velocity.enabled = true;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = .075f; velocity.z = -.035f;
                var noise = particles.noise; noise.enabled = true;
                noise.strength = .09f; noise.frequency = .55f; noise.scrollSpeed = .18f;
                var size = particles.sizeOverLifetime; size.enabled = true;
                size.size = new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.5f),new Keyframe(.45f,1.8f),new Keyframe(1,3.2f)));
                var colors = particles.colorOverLifetime; colors.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                    new[]{new GradientAlphaKey(0,0),new GradientAlphaKey(.9f,.12f),new GradientAlphaKey(.55f,.55f),new GradientAlphaKey(0,1)});
                colors.color = gradient;
        }

        public void TickWind()
        {
            if (_wind==null) return;
            foreach (var system in _systems)
            {
                if (system==null) continue;
                var flow=_wind(system.transform.position)*.18f;
                var velocity=system.velocityOverLifetime;
                velocity.enabled=true;
                velocity.space=ParticleSystemSimulationSpace.World;
                velocity.x=flow.x; velocity.z=flow.z;
            }
        }

        public void ResetTake()
        {
            foreach (var system in _systems)
            { system.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear); system.Play(); }
        }

        public void Dispose()
        {
            foreach (var system in _systems) if (system != null) UnityEngine.Object.Destroy(system.gameObject);
            foreach (var material in _materials) UnityEngine.Object.Destroy(material);
            _systems.Clear(); _materials.Clear();
        }
    }
}
