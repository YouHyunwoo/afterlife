using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Afterlife.Modules.Lighting
{
    public sealed class CrystalGlowEffect : IDisposable
    {
        private readonly Camera _camera;
        private readonly UniversalAdditionalCameraData _data;
        private readonly bool _hdr,_post;
        private readonly LayerMask _mask;
        private readonly GameObject _volumeObject;
        private readonly VolumeProfile _profile;
        private readonly List<Renderer> _renderers=new List<Renderer>();
        private readonly List<MaterialPropertyBlock> _original=new List<MaterialPropertyBlock>();
        public CrystalGlowEffect(Transform stage,Camera camera,IEnumerable<Renderer> targets=null)
        {
            _camera=camera; _data=camera.GetUniversalAdditionalCameraData();
            _hdr=camera.allowHDR; _post=_data.renderPostProcessing; _mask=_data.volumeLayerMask;
            camera.allowHDR=true; _data.renderPostProcessing=true; _data.volumeLayerMask=1<<31;
            _volumeObject=new GameObject("CrystalBloomVolume"); _volumeObject.layer=31;
            _volumeObject.transform.SetParent(stage,false);
            var volume=_volumeObject.AddComponent<Volume>(); volume.isGlobal=true; volume.priority=100;
            _profile=ScriptableObject.CreateInstance<VolumeProfile>();
            var bloom=_profile.Add<Bloom>(true); bloom.threshold.Override(1.15f); bloom.intensity.Override(.48f);
            bloom.scatter.Override(.52f); bloom.tint.Override(Color.white);
            volume.sharedProfile=_profile;
            if(targets==null)
            {
                var ruins=stage.Find("Environment/Ruins_Pivot");
                if (ruins==null) { Debug.LogWarning("Crystal ruins not found."); return; }
                targets=ruins.GetComponentsInChildren<Renderer>(true);
            }
            foreach (var renderer in targets)
            {
                var previous=new MaterialPropertyBlock(); renderer.GetPropertyBlock(previous);
                _renderers.Add(renderer); _original.Add(previous);
                var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                block.SetFloat("_CrystalGlow",3.5f); renderer.SetPropertyBlock(block);
            }
        }
        public void Dispose()
        {
            for (int i=0;i<_renderers.Count;i++) if (_renderers[i]!=null) { var block=new MaterialPropertyBlock();_renderers[i].GetPropertyBlock(block);block.SetFloat("_CrystalGlow",_original[i].GetFloat("_CrystalGlow"));_renderers[i].SetPropertyBlock(block); }
            if (_camera!=null) _camera.allowHDR=_hdr;
            if (_data!=null) { _data.renderPostProcessing=_post; _data.volumeLayerMask=_mask; }
            Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_volumeObject);
            foreach (var component in _profile.components) Afterlife.Modules.UnitySupport.ObjectLifetime.Release(component);
            Afterlife.Modules.UnitySupport.ObjectLifetime.Release(_profile);
        }
    }
}
