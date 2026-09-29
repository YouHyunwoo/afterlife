using Afterlife.Modules.Snow;
using Afterlife.Modules.Interaction;
using System.Collections.Generic;
using Afterlife.Modules.TargetContracts;
using UnityEngine;

namespace Afterlife.Prototype
{
    // 게임 상태를 소유하지 않는 눈 표현 어댑터. 메시/재질은 소유자가 먼저 해제한다.
    public sealed class PrototypeSnowTarget : MonoBehaviour, ISnowTarget
    {
        public Transform Visual => transform;
        public bool HasSnow => true;
        public bool IsBroken { get; set; }
        private readonly Vector4[] _hits=new Vector4[32];
        private int _count;
        private readonly List<SnowCapRenderer> _caps=new List<SnowCapRenderer>();
        private Renderer[] _renderers;
        private MaterialPropertyBlock _properties;
        private Matrix4x4 _lastMatrix;
        private bool _dirty=true;
        private PrototypeEnvironmentValues _settings=new PrototypeEnvironmentValues();
        public void Initialize(Renderer[] renderers,PrototypeEnvironmentValues settings=null){_settings=settings??new PrototypeEnvironmentValues();_renderers=renderers;_properties=new MaterialPropertyBlock();}
        public SnowCapRenderer AddCap(PickSurface source)
        {
            var cap=new SnowCapRenderer(source,this,source.name.StartsWith("Tree1_Mesh"));
            _caps.Add(cap);return cap;
        }
        public float SnowRemaining(Vector3 world)
        {
            Vector3 local=transform.InverseTransformPoint(world);float remaining=1;
            for(int i=0;i<_count;i++)remaining*=1-_hits[i].w*SnowErosion.Influence(local,(Vector3)_hits[i]);
            return remaining;
        }
        public bool SnowVisible(Vector3 world)=>SnowErosion.Visible(transform.InverseTransformPoint(world),SnowRemaining(world));
        public List<Vector3> Hit(Vector3 world)
        {
            var samples=new List<SnowCapRenderer.Sample>();
            foreach(var cap in _caps)cap.CollectSamples(world,samples);
            Vector3 local=transform.InverseTransformPoint(world);int slot=-1;float nearest=float.PositiveInfinity;
            for(int i=0;i<_count;i++){float distance=Vector3.Distance(local,(Vector3)_hits[i]);if(distance<nearest){nearest=distance;slot=i;}}
            if(nearest>_settings.SnowHitRadius && _count<_hits.Length){slot=_count++;_hits[slot]=new Vector4(local.x,local.y,local.z,0);}
            if(slot>=0)_hits[slot].w=Mathf.Min(1,_hits[slot].w+_settings.SnowHitPower);
            var particles=new List<Vector3>();
            foreach(var sample in samples)if(sample.Before-SnowRemaining(sample.MaskPosition)>.02f && particles.Count<_settings.SnowParticlesPerHit)particles.Add(sample.Position);
            foreach(var cap in _caps)cap.Refresh();
            _dirty=true;WriteProperties();return particles;
        }
        public bool WriteProperties()
        {
            if(_properties==null)return false;
            var matrix=transform.worldToLocalMatrix;if(!_dirty && matrix==_lastMatrix)return false;
            foreach(var renderer in _renderers)Write(renderer,matrix,0);
            foreach(var cap in _caps)if(cap.Renderer!=null)Write(cap.Renderer,matrix,1);
            _lastMatrix=matrix;_dirty=false;return true;
        }
        private void Write(Renderer renderer,Matrix4x4 matrix,float shell)
        {
            if(renderer==null)return;
            renderer.GetPropertyBlock(_properties);
            _properties.SetFloat("_LeafSnow",1);_properties.SetFloat("_LeafSnowPine",renderer.name.StartsWith("Tree1_Mesh")?1:0);_properties.SetFloat("_Snow",1);_properties.SetFloat("_SnowShell",shell);
            _properties.SetMatrix("_SnowWorldToLocal",matrix);_properties.SetInt("_SnowHitCount",_count);
            _properties.SetVectorArray("_SnowHits",_hits);renderer.SetPropertyBlock(_properties);
        }
        public void Release(){foreach(var cap in _caps)cap.Dispose();_caps.Clear();}
        private void OnDestroy()=>Release();
    }
}
