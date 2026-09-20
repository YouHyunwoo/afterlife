using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Afterlife.Devlog.Day01
{
    public sealed class ShowcaseCrystalGlow : IDisposable
    {
        private readonly Camera _camera;
        private readonly UniversalAdditionalCameraData _data;
        private readonly bool _hdr,_post;
        private readonly LayerMask _mask;
        private readonly GameObject _volumeObject;
        private readonly VolumeProfile _profile;
        private readonly List<Renderer> _renderers=new List<Renderer>();
        private readonly List<MaterialPropertyBlock> _original=new List<MaterialPropertyBlock>();
        public ShowcaseCrystalGlow(Transform stage,Camera camera)
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
            var ruins=stage.Find("Environment/Ruins_Pivot");
            if (ruins==null) { Debug.LogWarning("Crystal ruins not found."); return; }
            foreach (var renderer in ruins.GetComponentsInChildren<Renderer>(true))
            {
                var previous=new MaterialPropertyBlock(); renderer.GetPropertyBlock(previous);
                _renderers.Add(renderer); _original.Add(previous);
                var block=new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                block.SetFloat("_CrystalGlow",3.5f); renderer.SetPropertyBlock(block);
            }
        }
        public void Dispose()
        {
            for (int i=0;i<_renderers.Count;i++) if (_renderers[i]!=null) _renderers[i].SetPropertyBlock(_original[i]);
            if (_camera!=null) _camera.allowHDR=_hdr;
            if (_data!=null) { _data.renderPostProcessing=_post; _data.volumeLayerMask=_mask; }
            UnityEngine.Object.Destroy(_volumeObject);
            foreach (var component in _profile.components) UnityEngine.Object.Destroy(component);
            UnityEngine.Object.Destroy(_profile);
        }
    }
}
