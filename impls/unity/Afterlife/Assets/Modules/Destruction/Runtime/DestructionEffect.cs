using System;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Modules.Destruction
{
    // 작은 시각 파편 전용 풀. 씬 소유자가 Dispose하며 게임 규칙과 입력을 알지 않는다.
    public sealed class ImpactChipPool : IDisposable
    {
        private readonly Transform _parent;
        private readonly GameObject _prefab;
        private readonly Stack<GameObject> _free=new Stack<GameObject>();
        private readonly Dictionary<GameObject,Quaternion> _owned=new Dictionary<GameObject,Quaternion>();
        public ImpactChipPool(Transform parent,GameObject prefab) { _parent=parent; _prefab=prefab; }
        public GameObject Rent()
        {
            GameObject go;
            if (_free.Count>0) go=_free.Pop();
            else
            {
                go=_prefab!=null ? UnityEngine.Object.Instantiate(_prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.SetParent(_parent,false);
                var collider=go.GetComponent<Collider>();
                if (collider!=null) { collider.enabled=false; Afterlife.Modules.UnitySupport.ObjectLifetime.Release(collider); }
                _owned.Add(go,go.transform.localRotation);
            }
            go.transform.localRotation=_owned[go];
            go.SetActive(true);
            return go;
        }
        public void Return(GameObject go)
        {
            if (go==null) return;
            go.SetActive(false); _free.Push(go);
        }
        public void Dispose()
        {
            foreach (var go in _owned.Keys) if (go!=null) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(go);
            _owned.Clear(); _free.Clear();
        }
    }

    public enum DisappearMode { Sink, Dissolve }

    // 촬영용 운동: 물리 월드를 변경하지 않고 지면 높이 조회만 주입받는다.
    public sealed class DestructionEffect : IDisposable
    {
        private sealed class Fragment
        {
            public Transform Root;
            public Renderer Renderer;
            public Vector3 Velocity, Axis, Rest;
            public float Radius, Spin;
        }
        private readonly List<Fragment> _fragments=new List<Fragment>();
        private readonly MaterialPropertyBlock _properties=new MaterialPropertyBlock();
        private readonly Transform _visual;
        private readonly Renderer[] _renderers;
        private readonly Func<Vector3,float> _ground;
        private readonly DisappearMode _mode;
        private readonly bool _wood;
        private readonly Vector3 _start;
        private float _age, _sinkDistance;
        private bool _removing;
        public bool Completed { get; private set; }
        private readonly DestructionProfile _profile;
        private readonly Func<MeshFilter,bool> _fragmentSource;

        public DestructionEffect(Transform visual,Renderer[] renderers,bool wood,DisappearMode mode,
            Func<Vector3,float> ground,Vector3 away,DestructionProfile profile=null,Func<MeshFilter,bool> fragmentSource=null)
        {
            _visual=visual; _renderers=renderers; _wood=wood; _mode=mode; _ground=ground; _start=visual.position; _profile=profile; _fragmentSource=fragmentSource;
            if (!wood) SpawnFragments(away);
        }

        private void SpawnFragments(Vector3 away)
        {
            MeshFilter source=null;
            foreach (var filter in _visual.GetComponentsInChildren<MeshFilter>())
                if (filter.name!="SnowCap" && filter.sharedMesh!=null && (_fragmentSource==null || _fragmentSource(filter)) &&
                    (source==null || filter.sharedMesh.bounds.size.sqrMagnitude>source.sharedMesh.bounds.size.sqrMagnitude)) source=filter;
            if (source==null) return;
            var sourceRenderer=source.GetComponent<Renderer>();
            if (sourceRenderer==null) return;
            Bounds bounds=sourceRenderer.bounds;
            // FBX 원본 좌표 크기와 무관하게 조각 하나에 큰 Perlin 무늬가 생기도록 보정한다.
            // 화면에서만 축소하면 원본 메시의 촘촘한 무늬가 픽셀 잡음처럼 보인다.
            Vector3 meshSize=source.sharedMesh.bounds.size;
            float dissolveScale=3.5f/Mathf.Max(.001f,Mathf.Max(meshSize.x,Mathf.Max(meshSize.y,meshSize.z)));
            for (int i=0;i<(_profile!=null ? Mathf.Clamp(_profile.RockFragmentCount,2,12) : 6);i++)
            {
                float angle=i*2.39996f;
                var direction=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));
                var root=new GameObject("RockFragment_"+i).transform;
                root.SetParent(_visual.parent,false);
                var parentScale=root.parent.lossyScale;
                root.localScale=new Vector3(1/parentScale.x,1/parentScale.y,1/parentScale.z);
                root.position=bounds.center+direction*bounds.extents.magnitude*.22f;
                var model=new GameObject("RockMesh").transform; model.SetParent(root,false);
                model.rotation=source.transform.rotation;
                model.localScale=source.transform.lossyScale*(.30f+i*.023f);
                // 원점이 바닥에 있는 FBX도 회전 중심을 메시 중앙에 맞춘다.
                model.position-=model.TransformVector(source.sharedMesh.bounds.center);
                model.gameObject.AddComponent<MeshFilter>().sharedMesh=source.sharedMesh;
                var renderer=model.gameObject.AddComponent<MeshRenderer>(); renderer.sharedMaterials=sourceRenderer.sharedMaterials;
                sourceRenderer.GetPropertyBlock(_properties);
                _properties.SetFloat("_Flash",0);
                _properties.SetInt("_DamageMarkCountV1",0);
                _properties.SetFloat("_Dissolve",0);
                _properties.SetFloat("_DissolveScale",dissolveScale);
                renderer.SetPropertyBlock(_properties);
                float radius=Mathf.Max(.06f,renderer.bounds.extents.y);
                root.position=new Vector3(root.position.x,Mathf.Max(root.position.y,_ground(root.position)+radius),root.position.z);
                _fragments.Add(new Fragment { Root=root,Renderer=renderer,Radius=radius,Rest=root.position,
                    Velocity=direction*(.65f+i*.10f)+away*.4f+Vector3.up*(1.1f+i*.12f),
                    Axis=Vector3.Cross(Vector3.up,direction).normalized,Spin=120+i*23 });
            }
        }

        public void Tick(float delta)
        {
            if (Completed) return;
            _age+=delta;
            // 나무는 먼저 1.1초 동안 넘어지고 잠깐 머문 뒤 제거한다.
            float delay=_wood ? (_profile!=null ? _profile.TreeFallSeconds+_profile.TreeHoldSeconds : 1.65f)
                : (_profile!=null ? _profile.RockRollSeconds : 1.5f);
            float progress=Mathf.Clamp01((_age-delay)/(_profile!=null ? Mathf.Max(.1f,_profile.RemovalSeconds) : 1.15f));
            float eased=progress*progress*(3-2*progress);
            if (_wood)
            {
                if (_age<delay) return;
                if (!_removing)
                {
                    _removing=true;
                    float highest=_ground(_start);
                    foreach (var renderer in _renderers) if (renderer.enabled) highest=Mathf.Max(highest,renderer.bounds.max.y);
                    _sinkDistance=Mathf.Max(.5f,highest-_ground(_start)+.25f);
                }
                if (_mode==DisappearMode.Sink) _visual.position=_start+Vector3.down*(_sinkDistance*eased);
                foreach (var renderer in _renderers) Apply(renderer,progress,_ground(_start));
            }
            else
            {
                foreach (var fragment in _fragments)
                {
                    if (_age<delay)
                    {
                        fragment.Velocity+=Vector3.down*(7*delta);
                        fragment.Root.position+=fragment.Velocity*delta;
                        float floor=_ground(fragment.Root.position)+fragment.Radius;
                        if (fragment.Root.position.y<floor)
                        {
                            var p=fragment.Root.position; p.y=floor; fragment.Root.position=p;
                            fragment.Velocity.y=Mathf.Abs(fragment.Velocity.y)>.5f ? Mathf.Abs(fragment.Velocity.y)*.26f : 0;
                            float friction=Mathf.Exp(-4*delta);
                            fragment.Velocity.x*=friction; fragment.Velocity.z*=friction;
                            fragment.Spin*=friction;
                        }
                        fragment.Root.Rotate(fragment.Axis,fragment.Spin*delta,Space.World);
                        fragment.Rest=fragment.Root.position;
                    }
                    else
                    {
                        if (_mode==DisappearMode.Sink)
                            fragment.Root.position=fragment.Rest+Vector3.down*((fragment.Radius*2+.3f)*eased);
                        Apply(fragment.Renderer,progress,_ground(fragment.Rest));
                    }
                }
            }
            Completed=progress>=1;
        }

        private void Apply(Renderer renderer,float progress,float floor)
        {
            renderer.GetPropertyBlock(_properties);
            _properties.SetFloat("_Dissolve",_mode==DisappearMode.Dissolve ? progress : 0);
            _properties.SetFloat("_SinkClip",_mode==DisappearMode.Sink ? 1 : 0);
            _properties.SetFloat("_RemovalFloor",floor);
            renderer.SetPropertyBlock(_properties);
            if (progress>=1) renderer.enabled=false;
        }

        public void Dispose()
        {
            foreach (var fragment in _fragments)
                if (fragment.Root!=null) { fragment.Root.gameObject.SetActive(false); Afterlife.Modules.UnitySupport.ObjectLifetime.Release(fragment.Root.gameObject); }
            _fragments.Clear();
            foreach (var renderer in _renderers)
            {
                if (renderer==null) continue;
                renderer.GetPropertyBlock(_properties);
                _properties.SetFloat("_Dissolve",0); _properties.SetFloat("_SinkClip",0);
                renderer.SetPropertyBlock(_properties);
            }
        }
    }
}
