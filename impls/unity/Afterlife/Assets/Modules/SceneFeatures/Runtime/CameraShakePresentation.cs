using System;
using UnityEngine;
namespace Afterlife.Modules.SceneFeatures
{
    public sealed class CameraShakePresentation : IDisposable
    {
        private readonly Camera _camera;
        private Vector3 _offset,_applied;
        private bool _hasOffset;
        public CameraShakePresentation(Camera camera){_camera=camera;}
        public void Tick(float remaining,float strength)
        {
            if(_camera==null)return;
            RemoveOffset();
            _offset=_camera.transform.right*Mathf.Sin(remaining*160)*remaining*strength;
            _camera.transform.position+=_offset;_applied=_camera.transform.position;_hasOffset=true;
        }
        private void RemoveOffset(){if(_camera!=null&&_hasOffset&&_camera.transform.position==_applied)_camera.transform.position-=_offset;_hasOffset=false;}
        public void Dispose()=>RemoveOffset();
    }
}
