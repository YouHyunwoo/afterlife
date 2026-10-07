using Afterlife.Modules.Lighting;
using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private readonly Dictionary<int,SmokeEffect> _houseSmoke=new Dictionary<int,SmokeEffect>();
        private readonly List<int> _expiredSmoke=new List<int>();
        private Shader _smokeShader;
        private void UpdateHouseSmoke()
        {
            _expiredSmoke.Clear();
            foreach(var pair in _houseSmoke)
            {
                var body=_model.Find(pair.Key);
                if(body==null || !body.Alive || !body.Complete || body.MovingOut || !_objects.ContainsKey(pair.Key))_expiredSmoke.Add(pair.Key);
            }
            foreach(int id in _expiredSmoke){_houseSmoke[id].Dispose();_houseSmoke.Remove(id);}
            foreach(var body in _model.Bodies)
            {
                if(body.Kind!=Kind.House || !body.Alive || !body.Complete || body.MovingOut || _houseSmoke.ContainsKey(body.Id))continue;
                if(!_objects.TryGetValue(body.Id,out var house) || !TryChimneyOutlet(body.Id,out var outlet))continue;
                if(_smokeShader==null)_smokeShader=Resources.Load<Shader>("PrototypeSmoke");
                if(_smokeShader==null)return;
                var root=house.transform;
                _houseSmoke.Add(body.Id,new SmokeEffect(root,_=>outlet,null,
                    world=>_vegetationWind!=null?_vegetationWind.Sample(world):Vector3.zero,_smokeShader,candidate=>candidate==root));
            }
            foreach(var smoke in _houseSmoke.Values){smoke.SetPaused(_modal==Modal.Menu || _model.Frozen);smoke.TickWind();}
        }
        private bool TryChimneyOutlet(int id,out Vector3 outlet)
        {
            outlet=Vector3.zero;
            if(!_resourceSurfaces.TryGetValue(id,out var surfaces))return false;
            // Day01과 동일한 House 모델: 가장 높은 메시 꼭짓점들이 굴뚝 입구다.
            // 별도 적설 덮개와 접지 메시를 제외한 원본 정밀 표면만 사용한다.
            float highest=float.NegativeInfinity;
            foreach(var surface in surfaces)
            {
                var pick=surface.Pick;if(pick==null || pick.SnowCap || pick.Mesh==null)continue;
                foreach(var vertex in pick.Mesh.Vertices)highest=Mathf.Max(highest,pick.transform.TransformPoint(vertex).y);
            }
            if(float.IsNegativeInfinity(highest))return false;
            var unique=new HashSet<Vector3>();
            foreach(var surface in surfaces)
            {
                var pick=surface.Pick;if(pick==null || pick.SnowCap || pick.Mesh==null)continue;
                foreach(var vertex in pick.Mesh.Vertices)
                {
                    var world=pick.transform.TransformPoint(vertex);
                    if(highest-world.y<.001f && unique.Add(world))outlet+=world;
                }
            }
            if(unique.Count==0)return false;
            outlet/=unique.Count;return true;
        }
        private void ResetHouseSmoke()
        {
            foreach(var smoke in _houseSmoke.Values)smoke.Dispose();
            _houseSmoke.Clear();_expiredSmoke.Clear();
        }
    }
}
