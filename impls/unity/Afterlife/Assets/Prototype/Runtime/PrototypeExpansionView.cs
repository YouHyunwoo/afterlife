using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private int _connectionRevision=-1;
        private readonly Dictionary<int,int> _connectionMasks=new Dictionary<int,int>();
        private readonly Dictionary<int,PrototypeAnimalRig> _animals=new Dictionary<int,PrototypeAnimalRig>();
        private readonly RaycastHit[] _arrowHits=new RaycastHit[64];
        private Vector2 _buildScroll;
        private float ToolsBottom=>Mathf.Min(626,HudHeight-96);
        private bool ArrowBlocked(Body source,Body target)
        {
            Vector3 start=GroundPosition(source.Position,2.4f),end=GroundPosition(target.Position,.65f),delta=end-start;
            int count=Physics.RaycastNonAlloc(start,delta.normalized,_arrowHits,delta.magnitude);
            for(int i=0;i<count;i++)
            {
                var pick=_arrowHits[i].collider.GetComponentInParent<PrototypePick>();
                var body=pick==null?null:_model.Find(pick.Id);
                if(body!=null&&body!=source&&body!=target&&VillageModel.Obstacle(body))return true;
            }
            return false;
        }
        private void BuildExpansion(Transform root,Body body)
        {
            var stone=new Color(.42f,.47f,.50f);var darkStone=new Color(.27f,.31f,.34f);var wood=new Color(.39f,.23f,.12f);
            if(body.Kind==Kind.Wall)
            {
                Part(root,"Wall footing",PrimitiveType.Cube,new Vector3(0,.1f,0),new Vector3(.58f,.2f,.58f),darkStone);
                // 연결 방향에 맞춘 본체와 모서리 기둥은 RefreshConnections가 생성한다.
            }
            else if(body.Kind==Kind.ArrowTower||body.Kind==Kind.MagicTower)
            {
                Part(root,"Tower base",PrimitiveType.Cube,new Vector3(0,.14f,0),new Vector3(.94f,.28f,.94f),darkStone);
                Part(root,"Tower shaft",PrimitiveType.Cylinder,new Vector3(0,1.12f,0),new Vector3(.64f,.98f,.64f),stone);
                Part(root,"Battlement",PrimitiveType.Cylinder,new Vector3(0,2.06f,0),new Vector3(.95f,.13f,.95f),darkStone);
                for(int i=0;i<4;i++){float a=i*Mathf.PI/2;Part(root,"Merlon",PrimitiveType.Cube,new Vector3(Mathf.Sin(a)*.35f,2.3f,Mathf.Cos(a)*.35f),new Vector3(.23f,.30f,.23f),stone);}
                var turret=new GameObject("Turret").transform;turret.SetParent(root,false);turret.localPosition=Vector3.up*2.4f;
                if(body.Kind==Kind.ArrowTower)
                {
                    Part(turret,"Ballista rail",PrimitiveType.Cube,new Vector3(0,0,.13f),new Vector3(.12f,.10f,.62f),wood,false);
                    for(int side=-1;side<=1;side+=2){var arm=Part(turret,"Bow arm",PrimitiveType.Cube,new Vector3(side*.23f,0,.1f),new Vector3(.47f,.075f,.07f),wood,false);arm.transform.localRotation=Quaternion.Euler(0,side*22,0);}
                    Part(turret,"Loaded bolt",PrimitiveType.Cube,new Vector3(0,.08f,.20f),new Vector3(.035f,.035f,.6f),_cream,false);
                }
                else
                {
                    var crystal=Part(turret,"Lightning crystal",PrimitiveType.Cube,new Vector3(0,.15f,0),new Vector3(.28f,.40f,.28f),_cyan,false);crystal.transform.localRotation=Quaternion.Euler(0,45,35);
                    for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Part(turret,"Conductor",PrimitiveType.Cylinder,new Vector3(Mathf.Cos(a)*.22f,0,Mathf.Sin(a)*.22f),new Vector3(.055f,.26f,.055f),_gold,false);}
                }
            }
            else if(body.Kind==Kind.Chest)
            {
                Color chestColor=body.ChestGrade==ChestGrade.Common?wood:body.ChestGrade==ChestGrade.Rare?new Color(.16f,.32f,.48f):new Color(.38f,.18f,.48f);
                Color band=body.ChestGrade==ChestGrade.Common?new Color(.5f,.55f,.57f):body.ChestGrade==ChestGrade.Rare?_cyan:_gold;
                Part(root,"Chest body",PrimitiveType.Cube,new Vector3(0,.23f,0),new Vector3(.72f,.40f,.46f),chestColor);
                Part(root,"Rounded lid",PrimitiveType.Cylinder,new Vector3(0,.43f,0),new Vector3(.47f,.36f,.47f),Color.Lerp(chestColor,Color.white,.15f)).transform.localRotation=Quaternion.Euler(0,0,90);
                foreach(float x in new[]{-.24f,.24f})Part(root,"Iron band",PrimitiveType.Cube,new Vector3(x,.28f,-.238f),new Vector3(.07f,.47f,.025f),band,false);
                Part(root,"Latch",PrimitiveType.Cube,new Vector3(0,.32f,-.27f),new Vector3(.10f,.17f,.05f),band,false);
                if(body.ChestGrade!=ChestGrade.Common)Part(root,"Grade gem",PrimitiveType.Cube,new Vector3(0,.52f,0),new Vector3(.14f,.14f,.14f),band,false).transform.localRotation=Quaternion.Euler(0,45,30);
            }
            else
            {
                bool ruin=body.Kind==Kind.Ruins;
                Part(root,"Sacred platform",PrimitiveType.Cylinder,new Vector3(0,.10f,0),new Vector3(.95f,.1f,.95f),darkStone);
                if(ruin)
                {
                    for(int i=-1;i<=1;i+=2)Part(root,"Ancient pillar",PrimitiveType.Cylinder,new Vector3(i*.31f,.65f,0),new Vector3(.20f,.55f,.20f),stone);
                    Part(root,"Broken arch",PrimitiveType.Cube,new Vector3(0,1.19f,0),new Vector3(.82f,.16f,.24f),stone).transform.localRotation=Quaternion.Euler(0,0,5);
                    Part(root,"Ancient rune",PrimitiveType.Cube,new Vector3(0,.45f,-.15f),new Vector3(.22f,.30f,.06f),_gold,false);
                }
                else
                {
                    Part(root,"Altar plinth",PrimitiveType.Cube,new Vector3(0,.39f,0),new Vector3(.56f,.54f,.48f),stone);
                    Part(root,"Offering table",PrimitiveType.Cube,new Vector3(0,.7f,0),new Vector3(.85f,.13f,.70f),_cream);
                    var gem=Part(root,"Sacred crystal",PrimitiveType.Cube,new Vector3(0,.96f,0),new Vector3(.2f,.33f,.2f),body.Kind==Kind.AbandonedAltar?new Color(.35f,.55f,.45f):_cyan,false);gem.transform.localRotation=Quaternion.Euler(0,45,30);
                }
            }
        }
        private void RefreshConnections()
        {
            if(_connectionRevision==_model.TopologyRevision)return;_connectionRevision=_model.TopologyRevision;
            foreach(var body in _model.Bodies)
            {
                if(!body.Alive||body.Kind!=Kind.Wall&&body.Kind!=Kind.House&&body.Kind!=Kind.Altar||!_objects.TryGetValue(body.Id,out var root))continue;
                int mask=_model.Connections(body);if(_connectionMasks.TryGetValue(body.Id,out int old)&&old==mask)continue;_connectionMasks[body.Id]=mask;
                var previous=root.transform.Find("Connections");if(previous!=null){previous.gameObject.SetActive(false);Destroy(previous.gameObject);}
                var group=new GameObject("Connections").transform;group.SetParent(root.transform,false);
                bool wall=body.Kind==Kind.Wall,corner=(mask&5)!=0&&(mask&10)!=0;
                var color=wall?new Color(.43f,.48f,.51f):body.Kind==Kind.House?new Color(.70f,.58f,.40f):new Color(.42f,.47f,.50f);
                bool authored=_worldRigs.ContainsKey(body.Id);
                if(wall)
                {
                    if(!authored||!BuildWorldConnection(group,corner?"WallCorner":"WallPost",Vector3.forward))
                    {
                        Part(group,"Wall junction",corner?PrimitiveType.Cylinder:PrimitiveType.Cube,new Vector3(0,.65f,0),corner?new Vector3(.65f,.65f,.65f):new Vector3(.46f,1.3f,.46f),color);
                        if(corner)Part(group,"Round corner crown",PrimitiveType.Cylinder,new Vector3(0,1.33f,0),new Vector3(.75f,.09f,.75f),color);
                    }
                }
                for(int d=0;d<4;d++)
                {
                    if((mask&(1<<d))==0)continue;Vector3 dir=d==0?Vector3.forward:d==1?Vector3.right:d==2?Vector3.back:Vector3.left;
                    if(authored&&BuildWorldConnection(group,wall?"WallArm":body.Kind==Kind.House?"HouseBridge":"AltarPath",dir))continue;
                    var bridge=Part(group,wall?"Connected wall":"Covered connection",PrimitiveType.Cube,dir*.36f+Vector3.up*(wall?.58f:.47f),new Vector3(wall?.40f:.57f,wall?1.15f:.66f,.52f),color,wall);
                    bridge.transform.localRotation=Quaternion.LookRotation(dir);
                    if(wall)Part(group,"Crenellation",PrimitiveType.Cube,dir*.34f+Vector3.up*1.28f,new Vector3(.24f,.2f,.24f),color);
                    else
                    {
                        var top=Part(group,body.Kind==Kind.House?"Roof connection":"Altar stone path",PrimitiveType.Cube,dir*.39f+Vector3.up*(body.Kind==Kind.House?1.06f:.14f),new Vector3(.62f,.15f,.4f),body.Kind==Kind.House?new Color(.52f,.24f,.18f):_cream,false);top.transform.localRotation=Quaternion.LookRotation(dir);
                    }
                }
                foreach(var renderer in group.GetComponentsInChildren<Renderer>()){renderer.GetPropertyBlock(_block);renderer.gameObject.AddComponent<PrototypeTint>().Original=_block.GetColor("_BaseColor");}
                _renderCache[body.Id]=root.GetComponentsInChildren<Renderer>();
            }
        }
        private void BuildAnimal(GameObject root,Body body)
        {
            string name=new[]{"Deer","Rabbit","Boar","Fox"}[body.Variant%4];
            var asset=Resources.Load<GameObject>("AfterlifePrototype/Animals/"+name);
            if(asset==null){Debug.LogError("Missing Blender animal: "+name);return;}
            var visual=Instantiate(asset,root.transform,false);visual.name="Blender "+name;visual.transform.localScale=Vector3.one;
            var nose=visual.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.Split('.')[0]=="Nose");
            if(nose!=null){Vector3 forward=nose.position-visual.transform.position;forward.y=0;if(forward.sqrMagnitude>.0001f)visual.transform.rotation=Quaternion.FromToRotation(forward.normalized,Vector3.forward)*visual.transform.rotation;}
            var meshes=visual.GetComponentsInChildren<Renderer>();if(meshes.Length>0){Bounds bounds=meshes[0].bounds;foreach(var mesh in meshes)bounds.Encapsulate(mesh.bounds);float height=body.Variant%4==0?1.18f:body.Variant%4==1?.62f:.72f;visual.transform.localScale*=height/Mathf.Max(.01f,bounds.size.y);}
            var rig=root.AddComponent<PrototypeAnimalRig>();rig.Visual=visual.transform;
            rig.Head=visual.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.StartsWith("HeadPivot"));rig.Tail=visual.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.StartsWith("TailPivot"));
            if(rig.Head!=null)rig.HeadRest=rig.Head.localRotation;if(rig.Tail!=null)rig.TailRest=rig.Tail.localRotation;
            var legs=visual.GetComponentsInChildren<Transform>().Where(t=>t.name.StartsWith("Leg")).ToArray();rig.Legs=legs;rig.Rest=legs.Select(t=>t.localRotation).ToArray();
            foreach(var renderer in visual.GetComponentsInChildren<Renderer>())
            {
                var material=renderer.sharedMaterial;Color color=material!=null&&material.HasProperty("_BaseColor")?material.GetColor("_BaseColor"):material!=null&&material.HasProperty("_Color")?material.color:new Color(.55f,.35f,.2f);
                renderer.sharedMaterial=_surface;Tint(renderer,color);
            }
            var collider=root.AddComponent<BoxCollider>();collider.center=new Vector3(0,.37f,0);collider.size=new Vector3(.48f,.8f,.9f);
        }
        private void AnimateAnimal(Body body,PrototypeAnimalRig rig,Vector3 motion)
        {
            if(rig==null)return;bool moving=motion.sqrMagnitude>.00001f&&body.StunRemaining<=0;
            if(moving)rig.Cycle+=motion.magnitude*13;
            rig.Motion=Mathf.MoveTowards(rig.Motion,moving?1:0,Time.deltaTime*8);
            for(int i=0;i<rig.Legs.Length;i++){bool diagonal=rig.Legs[i].name.Contains("FL")||rig.Legs[i].name.Contains("BR");rig.Legs[i].localRotation=rig.Rest[i]*Quaternion.Euler(Mathf.Sin(rig.Cycle+(diagonal?0:Mathf.PI))*30*rig.Motion,0,0);}
            rig.Visual.localPosition=Vector3.up*Mathf.Abs(Mathf.Sin(rig.Cycle))*(body.Variant%4==1?.12f:.035f)*rig.Motion;
            if(rig.Head!=null)rig.Head.localRotation=rig.HeadRest*Quaternion.Euler(moving?Mathf.Sin(rig.Cycle)*5:Mathf.Sin(_effectTime*.8f+body.Id)*8,0,0);
            if(rig.Tail!=null)rig.Tail.localRotation=rig.TailRest*Quaternion.AngleAxis(Mathf.Sin(_effectTime*(moving?9:3)+body.Id)*12,Vector3.forward);
        }
    }
    public sealed class PrototypeAnimalRig:MonoBehaviour { public Transform Visual,Head,Tail;public Quaternion HeadRest,TailRest;public Transform[] Legs;public Quaternion[] Rest;public float Cycle,Motion; }
}
