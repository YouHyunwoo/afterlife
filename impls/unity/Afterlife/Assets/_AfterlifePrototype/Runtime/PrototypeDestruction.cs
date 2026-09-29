using System.Collections.Generic;
using Afterlife.Modules.Destruction;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private sealed class ResourceDestruction
        {
            public DestructionEffect Effect;
            public Transform Root;
            public Quaternion Rotation;
            public Vector3 Axis;
            public bool Wood;
            public float Age;
        }
        private readonly Dictionary<int,ResourceDestruction> _resourceDestructions=new Dictionary<int,ResourceDestruction>();
        private void PrepareResourceDestruction()
        {
            foreach(var pair in _reactions)
            {
                var reaction=pair.Value;
                if(reaction.Kind!=Kind.Tree && reaction.Kind!=Kind.Rock || _model.Find(pair.Key)!=null || _resourceDestructions.ContainsKey(pair.Key))continue;
                if(!_objects.TryGetValue(pair.Key,out var go) || !_renderCache.TryGetValue(pair.Key,out var renderers))continue;
                foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=false;
                bool wood=reaction.Kind==Kind.Tree;
                Vector3 away=reaction.Away.sqrMagnitude>.001f?reaction.Away:Vector3.forward;
                var effect=new DestructionEffect(go.transform,renderers,wood,DisappearMode.Dissolve,
                    world=>_terrainSurface!=null?_terrainSurface.Height(world.x,world.z):0,away,_settings.Destruction,
                    filter=>filter.sharedMesh!=_contactMesh && (filter.GetComponent<Afterlife.Modules.Interaction.PickSurface>()==null || !filter.GetComponent<Afterlife.Modules.Interaction.PickSurface>().SnowCap));
                if(!wood)foreach(var renderer in renderers)renderer.enabled=false;
                _resourceDestructions.Add(pair.Key,new ResourceDestruction{Effect=effect,Root=go.transform,
                    Rotation=reaction.Rotation,Axis=Vector3.Cross(Vector3.up,away).normalized,Wood=wood});
            }
        }
        private void UpdateResourceDestruction()
        {
            float dt=_modal==Modal.Menu?0:Time.deltaTime;
            foreach(var pair in _resourceDestructions)
            {
                var state=pair.Value;
                if(state.Effect.Completed)continue;
                state.Age+=dt;
                if(state.Wood)
                {
                    float fall=Mathf.Clamp01((state.Age-ImpactTiming.Hold)/(_settings.Destruction!=null?Mathf.Max(.1f,_settings.Destruction.TreeFallSeconds):1.1f));
                    state.Root.rotation=Quaternion.AngleAxis((_settings.Destruction!=null?_settings.Destruction.TreeFallDegrees:88)*fall*fall,state.Axis)*state.Rotation;
                    ResourceFlash(pair.Key,ImpactTiming.Flash(state.Age));
                }
                state.Effect.Tick(dt);
                // 원본 수관 MPB의 디졸브를 잎 카드에도 반영한다.
                if(_treeLeaves.TryGetValue(pair.Key,out var tree))tree.Revision++;
            }
        }
        private void RemoveResourceDestruction(int id)
        {
            if(!_resourceDestructions.TryGetValue(id,out var state))return;
            state.Effect.Dispose();_resourceDestructions.Remove(id);
        }
        private void ResetResourceDestruction()
        {
            foreach(var state in _resourceDestructions.Values)state.Effect.Dispose();
            _resourceDestructions.Clear();
        }
    }
}
