using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private bool BuildResourceTool(ToolEffect tool,string name)
        {
            if(tool.Kind==Kind.Chest)return true; // 상자는 도구 없이 오브젝트 반동으로 반응한다.
            var source=WorldAsset(name);
            if(source==null)return false;
            // Day01과 동일하게 FBX 루트 축/스케일을 보존한다. FrontMarker 회전을 적용하지 않는다.
            var art=Instantiate(source,tool.Pivot,false);
            art.transform.localPosition=Vector3.zero;
            art.transform.localRotation=source.transform.localRotation;
            art.transform.localScale=source.transform.localScale;
            foreach(var renderer in art.GetComponentsInChildren<Renderer>())
            {
                var materials=renderer.sharedMaterials;
                for(int i=0;i<materials.Length;i++)materials[i]=_worldPalette;
                renderer.sharedMaterials=materials;Tint(renderer,Color.white);
            }
            foreach(var marker in art.GetComponentsInChildren<Transform>(true))
                if(marker.name.Split('.')[0]=="ImpactPoint"){tool.ImpactPoint=marker;break;}
            if(tool.ImpactPoint==null)
            {
                // 기존 검/망치 FBX에는 마커가 없어 보존된 축 기준 메시 경계에서 접촉점을 만든다.
                var bounds=new Bounds();bool found=false;
                foreach(var filter in art.GetComponentsInChildren<MeshFilter>())
                {
                    if(filter.sharedMesh==null)continue;
                    var meshBounds=filter.sharedMesh.bounds;
                    for(int i=0;i<8;i++)
                    {
                        var corner=meshBounds.center+Vector3.Scale(meshBounds.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1));
                        var local=tool.Pivot.InverseTransformPoint(filter.transform.TransformPoint(corner));
                        if(!found){bounds=new Bounds(local,Vector3.zero);found=true;}else bounds.Encapsulate(local);
                    }
                }
                var marker=new GameObject("RuntimeImpactPoint").transform;marker.SetParent(tool.Pivot,false);
                bool sword=VillageModel.IsHostile(tool.Kind)||tool.Kind==Kind.Animal;
                bool hand=tool.Kind==Kind.Chest||tool.Kind==Kind.Treasure;
                float side=Mathf.Abs(bounds.min.x)>Mathf.Abs(bounds.max.x)?bounds.min.x:bounds.max.x;
                marker.localPosition=sword?new Vector3(bounds.center.x,bounds.max.y,bounds.center.z):
                    hand?bounds.center:new Vector3(side,Mathf.Lerp(bounds.min.y,bounds.max.y,.82f),bounds.center.z);
                tool.ImpactPoint=marker;
                if(sword)
                {
                    tool.SwordGrip=new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,.17f),bounds.center.z);
                    tool.SwordBlade=new Vector3(bounds.center.x,Mathf.Lerp(bounds.min.y,bounds.max.y,.80f),bounds.center.z);
                }
            }
            return true;
        }
        // 칼끝을 매 프레임 끌어 맞추지 않고 손잡이 위치를 고정하여 칼날이 원호를 그린다.
        private void PoseSwordFromGrip(ToolEffect tool,Vector3 contact,float age)
        {
            Quaternion ready,hit,finish;
            if(tool.SwingVariant==0)
            {ready=Quaternion.Euler(0,0,-85);hit=Quaternion.Euler(0,0,25);finish=Quaternion.Euler(0,0,90);}
            else if(tool.SwingVariant==1)
            {ready=Quaternion.Euler(0,0,85);hit=Quaternion.Euler(0,0,-25);finish=Quaternion.Euler(0,0,-90);}
            else
            {ready=Quaternion.Euler(-80,0,-8);hit=Quaternion.Euler(15,0,-8);finish=Quaternion.Euler(75,0,-8);}
            float windup=Mathf.Clamp01(age/.12f);
            // 초반은 짧게 버티고 접촉 직전에 가속, 접촉 정지 후 관성으로 끝까지 벤다.
            float swing=windup*windup*windup;
            float follow=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.165f,.285f,age));
            Quaternion rotation=tool.SwingBasis*Quaternion.Slerp(Quaternion.Slerp(ready,hit,swing),finish,follow);
            Vector3 grip=contact-(tool.SwingBasis*hit)*(tool.SwordBlade-tool.SwordGrip);
            tool.Pivot.localPosition=Vector3.zero;tool.Pivot.localRotation=Quaternion.identity;tool.Pivot.localScale=Vector3.one;
            tool.Root.transform.rotation=rotation;
            tool.Root.transform.position=grip-rotation*tool.SwordGrip;
        }
        private void PoseResourceTool(ToolEffect tool,Point point,float age)
        {
            if(tool.ManualSurface==null)
            {
                var center=GroundPosition(point,tool.Kind==Kind.Tree?.62f:VillageModel.IsBuildingKind(tool.Kind)?.65f:.42f);
                var direction=tool.SwingBasis*Vector3.forward;
                var ray=new Ray(center-direction*4,direction);
                if(TryResourceSurface(tool.Body,ray,out var surface,out float distance))
                {
                    tool.ManualSurface=surface;
                    tool.ManualLocalPoint=surface.transform.InverseTransformPoint(ray.GetPoint(distance));
                    tool.ManualLocalNormal=surface.transform.localToWorldMatrix.transpose.MultiplyVector(surface.HitNormal).normalized;
                }
            }
            Vector3 contact=GroundPosition(point,tool.Kind==Kind.Tree?.62f:VillageModel.IsBuildingKind(tool.Kind)?.65f:.42f);
            tool.HitSurface=tool.ManualSurface;
            if(tool.ManualSurface!=null)
            {
                contact=tool.ManualSurface.transform.TransformPoint(tool.ManualLocalPoint);
                tool.HitNormal=tool.ManualSurface.transform.worldToLocalMatrix.transpose.MultiplyVector(tool.ManualLocalNormal).normalized;
            }
            if(tool.ManualSurface==null)tool.HitNormal=-(tool.SwingBasis*Vector3.forward);
            tool.HitPosition=contact;
            if(tool.Kind==Kind.Chest){tool.Root.SetActive(false);return;}
            if(VillageModel.IsHostile(tool.Kind)||tool.Kind==Kind.Animal)
            {PoseSwordFromGrip(tool,contact,age);return;}
            float approach=Mathf.Clamp01(age/.12f);approach*=approach;
            float recovery=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f+Afterlife.Modules.Destruction.ImpactTiming.Hold,.285f,age));
            Vector3 offset=tool.Approach;
            float startAngle=-65,contactAngle=25,retreatAngle=-10;
            bool hand=tool.Kind==Kind.Treasure;
            if(VillageModel.IsBuildingKind(tool.Kind))
            {offset=tool.SwingBasis*new Vector3(.28f,.55f,-.18f);startAngle=-55;contactAngle=20;retreatAngle=-20;}
            else if(hand){offset=tool.SwingBasis*new Vector3(.15f,.25f,-.35f);startAngle=-15;contactAngle=5;retreatAngle=-10;}
            Vector3 position=contact+offset*(1-approach+recovery*.65f);
            float angle=Mathf.Lerp(Mathf.Lerp(startAngle,contactAngle,approach),retreatAngle,recovery);
            tool.Pivot.localPosition=Vector3.zero;tool.Pivot.localRotation=Quaternion.identity;tool.Pivot.localScale=Vector3.one;
            tool.Root.transform.rotation=tool.SwingBasis*Quaternion.Euler(0,0,angle);
            if(tool.ImpactPoint!=null)tool.Root.transform.position+=position-tool.ImpactPoint.position;
            else tool.Root.transform.position=position-tool.Root.transform.TransformVector(tool.Kind==Kind.Tree?new Vector3(.27f,.48f,0):new Vector3(.265f,.40f,0));
        }
    }
}
