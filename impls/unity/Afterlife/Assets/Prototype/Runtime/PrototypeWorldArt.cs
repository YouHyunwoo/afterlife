using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Afterlife.Prototype
{
    // Blender owns the geometry and articulation pivots; model timers own animation impact times.
    public sealed partial class PrototypeView
    {
        private Material _worldPalette;
        private readonly Dictionary<int,PrototypeWorldRig> _worldRigs=new Dictionary<int,PrototypeWorldRig>();
        private readonly Dictionary<string,GameObject> _worldAssets=new Dictionary<string,GameObject>();
        private readonly List<Mesh> _environmentMeshes=new List<Mesh>();
        private Mesh _contactMesh;
        private GameObject WorldAsset(string id)
        {
            // Tree0's split model is scoped to the prototype; the original FBX stays available to Day01.
            if (id == "Tree0")
            {
                var split = Resources.Load<GameObject>("AfterlifePrototype/World/Tree0_Split");
                if (split != null) return split;
            }
            return _settings.WorldVisuals != null
                ? _settings.WorldVisuals.Find(id)
                : Resources.Load<GameObject>("AfterlifePrototype/World/" + id);
        }
        private bool BuildWorldConnection(Transform parent,string name,Vector3 direction)
        {
            if(!_worldAssets.TryGetValue(name,out var source)){source=WorldAsset(name);_worldAssets[name]=source;}
            if(source==null||_worldPalette==null)return false;
            var wrapper=new GameObject(name).transform;wrapper.SetParent(parent,false);wrapper.localRotation=Quaternion.LookRotation(direction);
            var art=Instantiate(source,wrapper,false);var marker=art.GetComponentsInChildren<Transform>().FirstOrDefault(t=>t.name.Split('.')[0]=="FrontMarker");
            if(marker!=null){Vector3 forward=marker.position-art.transform.position;forward.y=0;art.transform.rotation=Quaternion.FromToRotation(forward.normalized,wrapper.forward)*art.transform.rotation;}
            foreach(var r in art.GetComponentsInChildren<Renderer>()){r.sharedMaterial=_worldPalette;Tint(r,Color.white);}
            return true;
        }
        private bool BuildAuthoredBody(GameObject root,Body body)
        {
            if(body.Kind==Kind.Animal)return false;
            string key=body.Kind.ToString();
            if(body.Kind==Kind.Tree||body.Kind==Kind.Rock)key+=body.Variant%4;
            if(body.Kind==Kind.Chest)key+=(int)body.ChestGrade;
            if(!_worldAssets.TryGetValue(key,out var asset)){asset=WorldAsset(key);_worldAssets[key]=asset;}
            if(asset==null){Debug.LogWarning("Missing authored world asset: "+key);return false;}
            if(_worldPalette==null){_worldPalette=new Material(_surface){name="Blender palette / shared vision"};_worldPalette.SetFloat("_VertexPalette",1);_worldPalette.SetFloat("_ZWrite",1);_materials.Add(_worldPalette);}
            var art=Instantiate(asset,root.transform,false);art.name="Blender "+key;
            if(body.Kind==Kind.Tree)art.transform.localScale*=Mathf.Max(.1f,_settings.TreeModelScale)*(1.15f+body.Variant%3*.13f)/1.28f;
            else if(body.Kind==Kind.Rock)art.transform.localScale*=Mathf.Max(.1f,_settings.RockModelScale)*(1.05f+body.Variant%3*.15f)/1.20f;
            var nodes=art.GetComponentsInChildren<Transform>();
            var marker=nodes.FirstOrDefault(n=>n.name.Split('.')[0]=="FrontMarker");
            if(marker!=null)
            {
                Vector3 forward=marker.position-art.transform.position;forward.y=0;
                if(forward.sqrMagnitude>.001f)art.transform.rotation=Quaternion.FromToRotation(forward.normalized,Vector3.forward)*art.transform.rotation;
            }
            if(body.Building||body.Kind==Kind.AbandonedAltar||body.Kind==Kind.Ruins)art.transform.Rotate(0,180,0,Space.World);
            foreach(var renderer in art.GetComponentsInChildren<Renderer>())
            {renderer.sharedMaterial=_worldPalette;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;Tint(renderer,Color.white);}
            var rig=root.AddComponent<PrototypeWorldRig>();rig.Art=art.transform;
            rig.Parts=nodes;rig.Rest=nodes.Select(n=>n.localRotation).ToArray();rig.RestPositions=nodes.Select(n=>n.localPosition).ToArray();
            rig.Visual=rig.Find("Visual");rig.Lid=rig.Find("Lid");rig.Turret=rig.Find("Turret");rig.Crystal=rig.Find("Crystal");
            if(rig.Turret!=null&&marker!=null)rig.TurretBasis=Quaternion.Inverse(Quaternion.LookRotation((marker.position-art.transform.position).normalized))*rig.Turret.rotation;
            _worldRigs[body.Id]=rig;
            var box=root.AddComponent<BoxCollider>();
            // Walkable production plots remain pickable without blocking physics actors.
            if(body.Building && !_model.Building(body.Kind).BlocksMovement) box.isTrigger=true;
            float height=body.Kind==Kind.Tree?1.6f:VillageModel.IsTower(body.Kind)?2.5f:body.Building?1.4f:body.Kind==Kind.Rock?.65f:body.Kind==Kind.Chest?.62f:body.Kind==Kind.Slime?.60f:body.Kind==Kind.Treasure?.35f:1.2f;
            box.center=new Vector3(0,height*.5f,0);box.size=new Vector3(body.Building?body.Width*.9f:body.Kind==Kind.Tree?.68f:.64f,height,body.Building?body.Depth*.9f:.64f);
            if(body.Kind==Kind.Research)
            {
                // The authored laboratory extends away from its origin. Fit the clickable footprint to the visible model.
                var renderers=art.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;
                    for(int i=1;i<renderers.Length;i++)bounds.Encapsulate(renderers[i].bounds);
                    var min=root.transform.InverseTransformPoint(bounds.min);
                    var max=root.transform.InverseTransformPoint(bounds.max);
                    box.center=new Vector3((min.x+max.x)*.5f,box.center.y,(min.z+max.z)*.5f);
                    box.size=new Vector3(Mathf.Max(box.size.x,Mathf.Abs(max.x-min.x)),box.size.y,
                        Mathf.Max(box.size.z,Mathf.Abs(max.z-min.z)));
                }
            }
            if(body.Kind==Kind.Tree||body.Kind==Kind.Rock)
            {
                var renderers=art.GetComponentsInChildren<Renderer>();
                if(renderers.Length>0)
                {
                    var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
                    box.center=root.transform.InverseTransformPoint(bounds.center);box.size=bounds.size;
                }
            }
            if(body.Kind==Kind.Farm||body.Kind==Kind.LargeTest){box.center=Vector3.up*.3f;box.size=new Vector3(body.Width*.95f,.6f,.95f);}
            AddContactShadow(root.transform,body.Building?body.Width*.46f:body.Kind==Kind.Tree?.41f:.30f,body.Building?body.Depth*.45f:.27f);
            return true;
        }
        private void AddContactShadow(Transform root,float width,float depth)
        {
            var go=new GameObject("Soft contact");go.transform.SetParent(root,false);
            go.transform.localScale=new Vector3(width,1,depth);
            if(_contactMesh!=null){go.AddComponent<MeshFilter>().sharedMesh=_contactMesh;go.AddComponent<MeshRenderer>().sharedMaterial=_worldPalette;Tint(go.GetComponent<Renderer>(),new Color(1,1,1,.55f));return;}
            const int sides=16;var vertices=new Vector3[sides+1];var colors=new Color[sides+1];var triangles=new int[sides*3];
            vertices[0]=new Vector3(0,-.047f,0);colors[0]=new Color(.08f,.10f,.07f,.22f);
            for(int i=0;i<sides;i++){float a=i*Mathf.PI*2/sides;vertices[i+1]=new Vector3(Mathf.Cos(a),-.047f,Mathf.Sin(a));colors[i+1]=new Color(.08f,.10f,.07f,0);int t=i*3;triangles[t]=0;triangles[t+1]=i+1;triangles[t+2]=(i+1)%sides+1;}
            // The shared object shader uses RGB vertex colours. A single soft-looking dark footprint grounds the silhouette.
            var mesh=new Mesh{name="Contact footprint"};mesh.vertices=vertices;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateNormals();_environmentMeshes.Add(mesh);
            _contactMesh=mesh;
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterial=_worldPalette;
            Tint(go.GetComponent<Renderer>(),new Color(1,1,1,.55f));
        }
        private void AnimateWorldBody(Body body,GameObject go,Vector3 motion)
        {
            if(!_worldRigs.TryGetValue(body.Id,out var rig)||rig==null)return;
            bool moving=motion.sqrMagnitude>.000001f&&body.StunRemaining<=0;
            if(moving)rig.Cycle+=motion.magnitude*(body.Kind==Kind.Slime?9:10);
            float dt=_modal==Modal.Menu?0:Time.deltaTime;
            rig.Motion=Mathf.MoveTowards(rig.Motion,moving?1:0,dt*8);
            if(rig.Visual!=null)
            {
                float s=Mathf.Sin(rig.Cycle),stride=s*30*rig.Motion;
                bool windup=body.PendingTarget!=0,attack=windup||body.AttackRecovery>0;
                float prepare=1-Mathf.Clamp01(body.AttackWindup/.18f),recover=1-Mathf.Clamp01(body.AttackRecovery/.18f);
                if(body.Kind==Kind.Slime)
                {
                    float cycle=Mathf.Repeat(rig.Cycle/(Mathf.PI*2)+body.Id*.137f,1);
                    float arc=cycle<.2f?0:Mathf.Sin((cycle-.2f)/.8f*Mathf.PI);
                    float compression=cycle<.2f?Mathf.Sin(cycle/.2f*Mathf.PI)*.23f:-arc*.13f;
                    float effort=attack?1:rig.Motion;
                    if(windup)compression=.24f*prepare;
                    rig.Visual.localScale=new Vector3(1+compression*.65f*effort,1+compression*.65f*effort,1-compression*effort);
                    rig.Visual.localPosition=rig.Position("Visual")+rig.Visual.parent.InverseTransformVector(Vector3.up*arc*.26f*rig.Motion);
                    if(body.AttackRecovery>0)rig.Visual.localPosition+=rig.Visual.parent.InverseTransformVector(V(body.Facing).normalized)*Mathf.Sin(recover*Mathf.PI)*.13f;
                    return;
                }
                rig.Pose("LeftLeg",Quaternion.Euler(stride,0,0));rig.Pose("RightLeg",Quaternion.Euler(-stride,0,0));
                float breathing=Mathf.Sin(_effectTime*2.4f+body.Id)*.006f;
                // The imported hierarchy's vertical axis is Blender Z. World displacement is transformed to its parent.
                rig.Visual.localPosition=rig.Position("Visual")+rig.Visual.parent.InverseTransformVector(Vector3.up*(Mathf.Abs(s)*.037f*rig.Motion+breathing));
                rig.Pose("Head",Quaternion.Euler(0,0,Mathf.Sin(rig.Cycle)*2*rig.Motion));
                rig.Pose("Tail",Quaternion.Euler(0,0,Mathf.Sin(rig.Cycle+1)*12));
                if(VillageModel.IsRanged(body.Kind))
                {
                    float lift=windup?Mathf.SmoothStep(0,1,prepare):body.AttackRecovery>0?1-recover:0;
                    rig.Pose("RightArm",Quaternion.Euler(-80*lift-stride*.4f*(1-lift),0,0));
                    rig.Pose("LeftArm",Quaternion.Euler(-67*lift+stride*.4f*(1-lift),0,!VillageModel.IsMagic(body.Kind)?-22*lift:0));
                    rig.Pose("Grip",Quaternion.Euler(80*lift,0,0));
                }
                else
                {
                    // Lift blade behind the shoulder, arc through the target at impact, then recover.
                    float anticipation=Mathf.Clamp01(prepare/.55f),swing=Mathf.SmoothStep(0,1,Mathf.Clamp01((prepare-.55f)/.45f));
                    float shoulder=windup?Mathf.Lerp(-145*anticipation,-70,swing):body.AttackRecovery>0?Mathf.Lerp(-70,0,Mathf.SmoothStep(0,1,recover)):0;
                    float wrist=windup?Mathf.Lerp(115*anticipation,150,swing):body.AttackRecovery>0?Mathf.Lerp(150,0,Mathf.SmoothStep(0,1,recover)):0;
                    rig.Pose("RightArm",Quaternion.Euler(attack?shoulder:-stride*.6f,0,attack?-14:0));
                    rig.Pose("Grip",Quaternion.Euler(wrist,0,0));
                    rig.Pose("LeftArm",Quaternion.Euler(attack?-28:stride*.5f,0,0));
                    rig.Pose("Visual",Quaternion.AngleAxis(attack?(windup?-prepare*9:(1-recover)*12):s*2*rig.Motion,Vector3.forward));
                }
            }
            if(rig.Turret!=null&&V(body.Facing).sqrMagnitude>.001f)
                rig.Turret.rotation=Quaternion.LookRotation(V(body.Facing))*rig.TurretBasis;
            if(rig.Crystal!=null)rig.Pose("Crystal",Quaternion.AngleAxis(_effectTime*30,Vector3.forward));
        }
        private void OpenWorldChest(GameObject go,float age)
        {
            var rig=go.GetComponent<PrototypeWorldRig>();if(rig==null||rig.Lid==null)return;
            float opening=Mathf.Clamp01(age/.46f),angle=Mathf.Lerp(0,105,1-Mathf.Pow(1-opening,3));
            angle+=Mathf.Sin(Mathf.Clamp01((age-.3f)/.35f)*Mathf.PI)*5;
            rig.Pose("Lid",Quaternion.Euler(-angle,0,0));
            float shrink=Mathf.Clamp01((age-.95f)/.45f);go.transform.localScale=Vector3.one*(1-shrink);
            var prize=rig.Find("ChestTreasure");
            if(prize!=null)prize.localPosition=rig.Position("ChestTreasure")+prize.parent.InverseTransformVector(Vector3.up*Mathf.Sin(Mathf.Clamp01(age/.95f)*Mathf.PI)*.30f);
        }
        private void DisposeWorldArt(){foreach(var mesh in _environmentMeshes)if(mesh!=null)Destroy(mesh);_environmentMeshes.Clear();}
    }
    public sealed class PrototypeWorldRig:MonoBehaviour
    {
        public Transform Art,Visual,Lid,Turret,Crystal;
        public Transform[] Parts;public Quaternion[] Rest;public Vector3[] RestPositions;public float Cycle,Motion;public Quaternion TurretBasis;
        private readonly Dictionary<string,int> _indices=new Dictionary<string,int>();
        private int Index(string name){if(_indices.TryGetValue(name,out int i))return i;i=System.Array.FindIndex(Parts,p=>p.name.Split('.')[0]==name);_indices[name]=i;return i;}
        public Transform Find(string name){int i=Index(name);return i<0?null:Parts[i];}
        public Quaternion Rotation(string name){int i=Index(name);return i<0?Quaternion.identity:Rest[i];}
        public Vector3 Position(string name){int i=Index(name);return i<0?Vector3.zero:RestPositions[i];}
        public void Pose(string name,Quaternion delta){int i=Index(name);if(i>=0)Parts[i].localRotation=Rest[i]*delta;}
    }
}
