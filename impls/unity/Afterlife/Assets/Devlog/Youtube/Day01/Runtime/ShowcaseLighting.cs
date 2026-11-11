using UnityEngine;
namespace Afterlife.Devlog.Day01
{
    public sealed class ShowcaseLighting : Afterlife.Modules.SceneFeatures.EnvironmentLighting
    {
        public ShowcaseLighting(Transform root,Camera camera,Rect bounds):base(root,camera,bounds,Sun(root),Windows(root)) { }
        public void SetStage(Transform stage)
        {
            Vector3 center=stage.TransformPoint(new Vector3(2.35f,0,2.1f));
            foreach(var child in stage.GetComponentsInChildren<Transform>(true))
                if(child.name=="House_Pivot" || child.name=="House_Blockout") { center=child.position;center.y=stage.position.y;break; }
            BindStage(stage,center);
        }
        private static Light Sun(Transform root) { foreach(var light in root.GetComponentsInChildren<Light>(true))if(light.type==LightType.Directional)return light; return null; }
        private static Renderer[] Windows(Transform root) { var list=new System.Collections.Generic.List<Renderer>();foreach(var r in root.GetComponentsInChildren<Renderer>(true))if(r.name.StartsWith("House_Mesh"))list.Add(r);return list.ToArray(); }
    }
}
