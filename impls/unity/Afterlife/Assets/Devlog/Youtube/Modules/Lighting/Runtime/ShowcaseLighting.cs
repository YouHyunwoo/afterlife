using System;
using UnityEngine;

namespace Afterlife.Devlog.Day01
{
    // 독립 조명 표현 모듈. 프로토타입 전역 시야/게임 상태를 변경하지 않는다.
    public sealed class ShowcaseLighting : IDisposable
    {
        private readonly Transform _root;
        private readonly Camera _camera;
        private readonly Transform _cursorPoint, _housePoint;
        private readonly Light _sun;
        private readonly Color _sunColor, _background;
        private readonly float _sunIntensity;
        private readonly float _oldNight;
        private readonly float _oldVisionTest,_oldCursorFade;
        private bool _hasCursor;
        private float? _filmBlend;
        public void SetFilmBlend(float? blend) { _filmBlend=blend; }
        private float _cursorFade;
        private const int TrailCapacity=32;
        private readonly Vector4[] _trail=new Vector4[TrailCapacity];
        private readonly float[] _trailAges=new float[TrailCapacity];
        private readonly Vector4[] _oldTrail;
        private readonly int _oldTrailCount;
        private int _trailCount;
        private float _sinceTrail;
        public float TrailSeconds=1.4f;
        public void ResetVisionTrail()
        {
            _trailCount=0;_sinceTrail=0;_cursorFade=0;_hasCursor=false;
            Shader.SetGlobalInt("_DevlogVisionTrailCount",0);
            Shader.SetGlobalFloat("_DevlogCursorFade",0);
        }
        private void PublishTrail()
        {
            Shader.SetGlobalVectorArray("_DevlogVisionTrail",_trail);
            Shader.SetGlobalInt("_DevlogVisionTrailCount",_trailCount);
        }
        private void AddTrail(Vector3 point)
        {
            var position=new Vector2(point.x,point.z);
            if(_trailCount>0)
            {
                var last=_trail[_trailCount-1];
                var previous=new Vector2(last.x,last.y);
                float distance=Vector2.Distance(position,previous);
                if(distance<.12f)
                { _trailAges[_trailCount-1]=0;last.w=1;_trail[_trailCount-1]=last;PublishTrail();return; }
                if(_sinceTrail<.06f && distance<.35f)return;
                // 빠르게 훑어도 시야 원 사이가 끊어지지 않도록 경로를 제한적으로 보간한다.
                int steps=distance<CursorRadius*2?Mathf.Clamp(Mathf.CeilToInt(distance/Mathf.Max(.25f,CursorRadius*.35f)),1,8):1;
                for(int i=1;i<=steps;i++)PushTrail(Vector2.Lerp(previous,position,i/(float)steps));
            }
            else PushTrail(position);
            _sinceTrail=0;PublishTrail();
        }
        private void PushTrail(Vector2 position)
        {
            if(_trailCount==TrailCapacity)
            {
                Array.Copy(_trail,1,_trail,0,TrailCapacity-1);
                Array.Copy(_trailAges,1,_trailAges,0,TrailCapacity-1);_trailCount--;
            }
            _trail[_trailCount]=new Vector4(position.x,position.y,CursorRadius,1);
            _trailAges[_trailCount++]=0;
        }
        public bool VisionTest { get; private set; }
        public void SetVisionTest(bool enabled)
        {
            if(VisionTest==enabled)return;
            VisionTest=enabled;ResetVisionTrail();
            Shader.SetGlobalFloat("_DevlogVisionTest",enabled?1:0);
            Shader.SetGlobalFloat("_DevlogCursorFade",0);
        }
        private readonly Vector4 _oldCursor, _oldHouse;
        private Transform _stage;
        private float _blend;
        public bool Night { get; private set; }
        public float Height = .8f, CursorRadius = 3.2f, HouseRadius = 3f;

        public ShowcaseLighting(Transform root, Camera camera)
        {
            _root = root; _camera = camera; _background = camera.backgroundColor;
            _oldTrail=Shader.GetGlobalVectorArray("_DevlogVisionTrail");
            _oldTrailCount=Shader.GetGlobalInt("_DevlogVisionTrailCount");
            Shader.SetGlobalInt("_DevlogVisionTrailCount",0);
            _oldNight = Shader.GetGlobalFloat("_DevlogNight");
            _oldVisionTest=Shader.GetGlobalFloat("_DevlogVisionTest");
            _oldCursorFade=Shader.GetGlobalFloat("_DevlogCursorFade");
            Shader.SetGlobalFloat("_DevlogVisionTest",0);
            Shader.SetGlobalFloat("_DevlogCursorFade",0);
            _oldCursor = Shader.GetGlobalVector("_DevlogCursorVision");
            _oldHouse = Shader.GetGlobalVector("_DevlogHouseVision");
            foreach (var light in root.GetComponentsInChildren<Light>(true))
                if (light.type == LightType.Directional) { _sun = light; break; }
            if (_sun != null) { _sunColor = _sun.color; _sunIntensity = _sun.intensity; }
            _cursorPoint = MakePoint("CursorVisionPoint");
            _housePoint = MakePoint("HouseVisionPoint");
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("House_Mesh")) continue;
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                block.SetFloat("_HouseWindows",1); renderer.SetPropertyBlock(block);
            }
        }

        private Transform MakePoint(string name)
        {
            var point = new GameObject(name).transform;
            point.SetParent(_root,false); return point;
        }

        public void SetStage(Transform stage)
        {
            _stage = stage;ResetVisionTrail();
            Vector3 center = stage.TransformPoint(new Vector3(2.35f,0,2.1f));
            foreach (var child in stage.GetComponentsInChildren<Transform>(true))
                if (child.name == "House_Pivot" || child.name == "House_Blockout")
                { center = child.position; center.y = stage.position.y; break; }
            _housePoint.position = center + Vector3.up*Height;
            Shader.SetGlobalVector("_DevlogCursorVision",Vector4.zero);
            PublishHouse();
        }

        public void SetNight(bool night, bool immediate = false)
        {
            Night = night;
            if (immediate) { _blend = night ? 1 : 0; Tick(0); }
        }

        public void Toggle() { Night = !Night; }

        public void Tick(float delta)
        {
            _sinceTrail+=delta;
            if(VisionTest)
            {
                int alive=0;float lifetime=Mathf.Max(.1f,TrailSeconds);
                for(int i=0;i<_trailCount;i++)
                {
                    float age=_trailAges[i]+delta;if(age>=lifetime)continue;
                    var entry=_trail[i];entry.w=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(lifetime*.2f,lifetime,age));
                    _trail[alive]=entry;_trailAges[alive++]=age;
                }
                _trailCount=alive;PublishTrail();
            }
            _cursorFade=Mathf.MoveTowards(_cursorFade,_hasCursor?1:0,delta/(_hasCursor?.15f:.8f));
            Shader.SetGlobalFloat("_DevlogCursorFade",_cursorFade);
            _blend = _filmBlend.HasValue ? Mathf.Clamp01(_filmBlend.Value) : Mathf.MoveTowards(_blend,Night ? 1 : 0,delta*2);
            Shader.SetGlobalFloat("_DevlogNight",_blend);
            _camera.backgroundColor = Color.Lerp(_background,new Color(.006f,.009f,.022f),_blend);
            if (_sun != null)
            {
                _sun.color = Color.Lerp(_sunColor,new Color(.38f,.48f,.78f),_blend);
                _sun.intensity = Mathf.Lerp(_sunIntensity,_sunIntensity*.28f,_blend);
            }
            PublishHouse();
        }

        private void PublishHouse()
        {
            Vector3 p = _housePoint.position;
            Shader.SetGlobalVector("_DevlogHouseVision",new Vector4(p.x,p.y,p.z,HouseRadius));
        }

        public void UpdateCursor(Ray? ray)
        {
            _hasCursor=false;
            if (_stage == null || !ray.HasValue || !new Plane(Vector3.up,_stage.position).Raycast(ray.Value,out float distance))
            { if(!VisionTest)Shader.SetGlobalVector("_DevlogCursorVision",Vector4.zero); return; }
            Vector3 floor = ray.Value.GetPoint(distance);
            Vector3 local = _stage.InverseTransformPoint(floor);
            if (local.x < -6 || local.x > 6 || local.z < -5 || local.z > 5)
            { if(!VisionTest)Shader.SetGlobalVector("_DevlogCursorVision",Vector4.zero); return; }
            // 시야 원점은 지면 교차점보다 높게 둬 수목/돌의 높이에도 반경이 적용된다.
            _hasCursor=true;
            if(VisionTest)AddTrail(floor);
            _cursorPoint.position = floor + Vector3.up*Height;
            Vector3 p = _cursorPoint.position;
            Shader.SetGlobalVector("_DevlogCursorVision",new Vector4(p.x,p.y,p.z,CursorRadius));
        }

        public void Dispose()
        {
            if(_oldTrail!=null && _oldTrail.Length>0)Shader.SetGlobalVectorArray("_DevlogVisionTrail",_oldTrail);
            Shader.SetGlobalInt("_DevlogVisionTrailCount",_oldTrailCount);
            Shader.SetGlobalFloat("_DevlogNight",_oldNight);
            Shader.SetGlobalFloat("_DevlogVisionTest",_oldVisionTest);
            Shader.SetGlobalFloat("_DevlogCursorFade",_oldCursorFade);
            Shader.SetGlobalVector("_DevlogCursorVision",_oldCursor);
            Shader.SetGlobalVector("_DevlogHouseVision",_oldHouse);
            if (_camera != null) _camera.backgroundColor = _background;
            if (_sun != null) { _sun.color = _sunColor; _sun.intensity = _sunIntensity; }
            foreach (var renderer in _root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!renderer.name.StartsWith("House_Mesh")) continue;
                var block = new MaterialPropertyBlock(); renderer.GetPropertyBlock(block);
                block.SetFloat("_HouseWindows",0); renderer.SetPropertyBlock(block);
            }
            UnityEngine.Object.Destroy(_cursorPoint.gameObject);
            UnityEngine.Object.Destroy(_housePoint.gameObject);
        }
    }
}
