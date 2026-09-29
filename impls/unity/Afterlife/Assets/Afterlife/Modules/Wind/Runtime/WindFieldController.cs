using System;
using UnityEngine;

namespace Afterlife.Modules.Wind
{
    // CPU 파티클과 GPU 식생이 같은 방향/공간 위상의 바람을 사용한다.
    public sealed class WindFieldController : IDisposable
    {
        private readonly Vector4 _previous = Shader.GetGlobalVector("_AfterlifeWind");
        private readonly Vector2 _direction = new Vector2(.84f,-.54f).normalized;
        private float _time, _strength = 1;
        public WindFieldController() { Tick(Time.unscaledTime); }
        public void Tick(float time, float strength = 1)
        {
            _time = time; _strength = Mathf.Max(0,strength);
            Shader.SetGlobalVector("_AfterlifeWind",new Vector4(_direction.x,_direction.y,time,_strength));
        }
        public Vector3 Sample(Vector3 point)
        {
            float phase=point.x*_direction.x+point.z*_direction.y;
            float gust=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.68f,1,Mathf.Sin(_time*.55f-phase*.65f)));
            float strength=.55f+.32f*Mathf.Sin(_time*1.6f-phase*.8f)+.65f*gust;
            return new Vector3(_direction.x,0,_direction.y)*strength*_strength;
        }
        public void Dispose() { Shader.SetGlobalVector("_AfterlifeWind",_previous); }
    }
}
