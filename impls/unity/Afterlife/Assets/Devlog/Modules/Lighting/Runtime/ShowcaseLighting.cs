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
        private readonly Vector4 _oldCursor, _oldHouse;
        private Transform _stage;
        private float _blend;
        public bool Night { get; private set; }
        public float Height = .8f, CursorRadius = 3.2f, HouseRadius = 3f;

        public ShowcaseLighting(Transform root, Camera camera)
        {
            _root = root; _camera = camera; _background = camera.backgroundColor;
            _oldNight = Shader.GetGlobalFloat("_DevlogNight");
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
            _stage = stage;
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
            _blend = Mathf.MoveTowards(_blend,Night ? 1 : 0,delta*2);
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
            if (_stage == null || !ray.HasValue || !new Plane(Vector3.up,_stage.position).Raycast(ray.Value,out float distance))
            { Shader.SetGlobalVector("_DevlogCursorVision",Vector4.zero); return; }
            Vector3 floor = ray.Value.GetPoint(distance);
            Vector3 local = _stage.InverseTransformPoint(floor);
            if (local.x < -6 || local.x > 6 || local.z < -5 || local.z > 5)
            { Shader.SetGlobalVector("_DevlogCursorVision",Vector4.zero); return; }
            // 시야 원점은 지면 교차점보다 높게 둬 수목/돌의 높이에도 반경이 적용된다.
            _cursorPoint.position = floor + Vector3.up*Height;
            Vector3 p = _cursorPoint.position;
            Shader.SetGlobalVector("_DevlogCursorVision",new Vector4(p.x,p.y,p.z,CursorRadius));
        }

        public void Dispose()
        {
            Shader.SetGlobalFloat("_DevlogNight",_oldNight);
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
