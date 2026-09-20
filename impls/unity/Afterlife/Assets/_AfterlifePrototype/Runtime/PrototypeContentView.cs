using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private Mesh _cone;
        private sealed class FloatingAmount { public Vector3 Position;public string Text;public float Born;public float Offset;public FeedbackKind Kind; }
        private readonly List<FloatingAmount> _amounts=new List<FloatingAmount>();
        private sealed class ShotView { public GameObject Root;public int Id,Style;public bool Used;public LineRenderer Bolt;public Vector3 Previous; }
        private readonly List<ShotView> _shotViews=new List<ShotView>();
        private static string ResourceName(Body body)
        {
            if(body.Kind==Kind.Tree)return body.Variant==0?"둥근나무":body.Variant==1?"침엽수":body.Variant==2?"자작나무":"참나무";
            if(body.Kind==Kind.Animal)return new[]{"사슴","토끼","멧돼지","여우"}[body.Variant%4];
            if(body.Kind==Kind.Rock)return body.Variant==0?"일반 바위":body.Variant==1?"층리암":body.Variant==2?"수정 바위":"화강암";
            return VillageModel.Name(body.Kind);
        }
        private GameObject Cone(Transform parent,string name,Vector3 position,Vector3 scale,Color color)
        {
            if(_cone==null)
            {
                var vertices=new List<Vector3>();var triangles=new List<int>();
                for(int i=0;i<6;i++)
                {
                    float a=i*Mathf.PI/3,b=(i+1)*Mathf.PI/3;int n=vertices.Count;
                    vertices.Add(new Vector3(Mathf.Cos(a)*.5f,0,Mathf.Sin(a)*.5f));vertices.Add(Vector3.up);vertices.Add(new Vector3(Mathf.Cos(b)*.5f,0,Mathf.Sin(b)*.5f));
                    triangles.Add(n);triangles.Add(n+1);triangles.Add(n+2);
                }
                _cone=new Mesh{name="Faceted cone"};_cone.SetVertices(vertices);_cone.SetTriangles(triangles,0);_cone.RecalculateNormals();
            }
            var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;go.transform.localScale=scale;
            go.AddComponent<MeshFilter>().sharedMesh=_cone;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=_surface;Tint(renderer,color);return go;
        }
        private void BuildResource(Transform root,Body body)
        {
            if(body.Kind==Kind.Tree)
            {
                bool birch=body.Variant==2;
                Part(root,"Trunk",PrimitiveType.Cylinder,new Vector3(0,.46f,0),new Vector3(birch?.14f:.2f,.46f,birch?.14f:.2f),birch?new Color(.82f,.84f,.76f):new Color(.38f,.23f,.12f));
                if(body.Variant==1)
                {
                    for(int i=0;i<3;i++)Cone(root,"Pine layer",new Vector3(0,.5f+i*.3f,0),new Vector3(.85f-i*.18f,.65f,.85f-i*.18f),new Color(.14f,.34f+i*.035f,.18f));
                }
                else if(birch)
                {
                    for(int i=0;i<4;i++)Part(root,"Birch stripe",PrimitiveType.Cube,new Vector3(0,.18f+i*.18f,.077f),new Vector3(.09f,.032f,.015f),new Color(.21f,.23f,.19f),false);
                    Part(root,"Birch crown",PrimitiveType.Sphere,new Vector3(0,1.08f,0),new Vector3(.62f,.76f,.62f),new Color(.56f,.65f,.24f));
                }
                else if(body.Variant==3)
                {
                    for(int i=0;i<3;i++){float a=i*Mathf.PI*2/3;Part(root,"Oak crown",PrimitiveType.Sphere,new Vector3(Mathf.Cos(a)*.2f,1.02f+(i%2)*.13f,Mathf.Sin(a)*.2f),new Vector3(.57f,.62f,.57f),new Color(.21f,.42f,.18f));}
                }
                else Part(root,"Round crown",PrimitiveType.Sphere,new Vector3(0,1.05f,0),new Vector3(.8f,1.1f,.8f),new Color(.24f,.48f,.27f));
            }
            else
            {
                Color grey=new Color(.52f,.57f,.57f);
                if(body.Variant==1)
                {
                    for(int i=0;i<3;i++){var slab=Part(root,"Slate layer",PrimitiveType.Cube,new Vector3((i%2)*.05f,.12f+i*.13f,0),new Vector3(.66f-i*.10f,.16f,.55f-i*.06f),new Color(.37f+i*.045f,.42f+i*.035f,.46f));slab.transform.localRotation=Quaternion.Euler(0,i*14,4);}
                }
                else if(body.Variant==2)
                {
                    Part(root,"Crystal bed",PrimitiveType.Cube,new Vector3(0,.1f,0),new Vector3(.6f,.2f,.55f),grey);
                    for(int i=0;i<3;i++){var crystal=Cone(root,"Crystal",new Vector3((i-1)*.16f,.12f,(i%2)*.1f),new Vector3(.22f,.4f+(i%2)*.2f,.22f),new Color(.44f,.65f,.72f));crystal.transform.localRotation=Quaternion.Euler(8,i*23,(i-1)*14);}
                }
                else if(body.Variant==3)
                {
                    Part(root,"Granite",PrimitiveType.Sphere,new Vector3(0,.26f,0),new Vector3(.7f,.57f,.6f),new Color(.64f,.56f,.48f));
                    for(int i=0;i<4;i++)Part(root,"Granite grain",PrimitiveType.Cube,new Vector3((i%2-.5f)*.25f,.43f,(i/2-.5f)*.22f),new Vector3(.08f,.035f,.08f),grey,false);
                }
                else{var rock=Part(root,"Stone",PrimitiveType.Cube,new Vector3(0,.25f,0),new Vector3(.6f,.5f,.55f),grey);rock.transform.localRotation=Quaternion.Euler(8,28,5);}
            }
        }
        private Transform Joint(Transform parent,string name,Vector3 position)
        {var go=new GameObject(name);go.transform.SetParent(parent,false);go.transform.localPosition=position;return go.transform;}
        private void BuildFighter(GameObject root,Body body)
        {
            var rig=root.AddComponent<PrototypeWeapon>();rig.Visual=Joint(root.transform,"Animated body",Vector3.zero);
            var parent=rig.Visual;
            bool orc=body.Kind==Kind.Orc,lizard=body.Kind==Kind.Lizard,goblin=body.Hostile&&!orc&&!lizard;
            Color skin=body.Friendly?new Color(.78f,.61f,.44f):orc?new Color(.36f,.43f,.16f):lizard?new Color(.2f,.5f,.47f):new Color(.42f,.64f,.24f);
            Color clothes=body.Kind==Kind.GoblinShaman?new Color(.44f,.22f,.53f):body.Kind==Kind.Mage?new Color(.3f,.28f,.67f):body.Kind==Kind.Archer?new Color(.27f,.43f,.24f):body.Kind==Kind.GoblinWarrior||orc?new Color(.35f,.3f,.28f):body.Friendly?new Color(.29f,.43f,.66f):new Color(.47f,.27f,.15f);
            float width=orc?.44f:.31f;
            Part(parent,"Torso",PrimitiveType.Cube,new Vector3(0,.53f,0),new Vector3(width,.38f,.25f),clothes);
            Part(parent,"Head",PrimitiveType.Cube,new Vector3(0,.89f,0),new Vector3(orc?.39f:.3f,.28f,.28f),skin);
            Part(parent,"Eye left",PrimitiveType.Cube,new Vector3(-.085f,.91f,.15f),new Vector3(.045f,.035f,.02f),body.Hostile?new Color(.9f,.32f,.12f):Color.black,false);
            Part(parent,"Eye right",PrimitiveType.Cube,new Vector3(.085f,.91f,.15f),new Vector3(.045f,.035f,.02f),body.Hostile?new Color(.9f,.32f,.12f):Color.black,false);
            if(goblin)
            {
                for(int side=-1;side<=1;side+=2){var ear=Cone(parent,"Pointed ear",new Vector3(side*.14f,.88f,0),new Vector3(.16f,.2f,.10f),skin);ear.transform.localRotation=Quaternion.Euler(0,0,-side*75);}
            }
            if(lizard)
            {
                Part(parent,"Snout",PrimitiveType.Cube,new Vector3(0,.84f,.2f),new Vector3(.22f,.13f,.22f),skin);
                for(int i=0;i<3;i++)Part(parent,"Tail",PrimitiveType.Cube,new Vector3(0,.3f-i*.04f,-.2f-i*.12f),new Vector3(.16f-i*.04f,.12f-i*.02f,.18f),skin,false);
            }
            if(orc)for(int side=-1;side<=1;side+=2)Cone(parent,"Tusk",new Vector3(side*.12f,.77f,.15f),new Vector3(.07f,.18f,.07f),new Color(.9f,.85f,.65f));
            if(body.Kind==Kind.GoblinWarrior||body.Kind==Kind.Knight||body.Kind==Kind.Warrior)
                Part(parent,"Helmet",PrimitiveType.Cube,new Vector3(0,1.01f,-.02f),new Vector3(.34f,.11f,.3f),new Color(.54f,.58f,.58f),false);
            if(body.Kind==Kind.GoblinShaman||body.Kind==Kind.Mage)Cone(parent,"Caster hat",new Vector3(0,1.01f,0),new Vector3(.39f,.36f,.39f),clothes);
            rig.LeftLeg=Joint(parent,"Left leg pivot",new Vector3(.105f,.34f,0));rig.RightLeg=Joint(parent,"Right leg pivot",new Vector3(-.105f,.34f,0));
            Part(rig.LeftLeg,"Boot",PrimitiveType.Cube,new Vector3(0,-.15f,.02f),new Vector3(.13f,.28f,.17f),new Color(.22f,.18f,.14f));
            Part(rig.RightLeg,"Boot",PrimitiveType.Cube,new Vector3(0,-.15f,.02f),new Vector3(.13f,.28f,.17f),new Color(.22f,.18f,.14f));
            rig.LeftArm=Joint(parent,"Left shoulder",new Vector3(width*.65f,.68f,0));rig.RightArm=Joint(parent,"Right shoulder",new Vector3(-width*.65f,.68f,0));
            Part(rig.LeftArm,"Left arm",PrimitiveType.Cube,new Vector3(0,-.13f,0),new Vector3(.10f,.27f,.12f),skin,false);
            Part(rig.RightArm,"Right arm",PrimitiveType.Cube,new Vector3(0,-.13f,0),new Vector3(.10f,.27f,.12f),skin,false);
            rig.Grip=Joint(rig.RightArm,"Weapon grip",new Vector3(0,-.24f,.02f));
            if(VillageModel.IsMagic(body.Kind))
            {
                Part(rig.Grip,"Staff",PrimitiveType.Cylinder,new Vector3(0,.17f,0),new Vector3(.045f,.4f,.045f),new Color(.38f,.23f,.12f),false);
                Part(rig.Grip,"Focus",PrimitiveType.Sphere,new Vector3(0,.62f,0),new Vector3(.16f,.18f,.16f),body.Hostile?new Color(.85f,.3f,.85f):_cyan,false);
            }
            else if(VillageModel.IsRanged(body.Kind))
            {
                for(int i=0;i<3;i++){var limb=Part(rig.Grip,"Bow limb",PrimitiveType.Cube,new Vector3(0,(i-1)*.18f,.12f-Mathf.Abs(i-1)*.10f),new Vector3(.055f,.23f,.055f),new Color(.55f,.34f,.15f),false);limb.transform.localRotation=Quaternion.Euler((i-1)*35,0,0);}
                Part(rig.Grip,"Bow string",PrimitiveType.Cube,Vector3.zero,new Vector3(.012f,.58f,.012f),new Color(.84f,.8f,.65f),false);
            }
            else
            {
                Part(rig.Grip,"Handle",PrimitiveType.Cube,new Vector3(0,.10f,0),new Vector3(.065f,.2f,.065f),new Color(.35f,.2f,.1f),false);
                if(orc)Part(rig.Grip,"Heavy club",PrimitiveType.Cube,new Vector3(0,.44f,0),new Vector3(.22f,.44f,.17f),new Color(.41f,.32f,.22f),false);
                else
                {
                    Part(rig.Grip,"Guard",PrimitiveType.Cube,new Vector3(0,.19f,0),new Vector3(.22f,.045f,.065f),_gold,false);
                    Part(rig.Grip,"Blade",PrimitiveType.Cube,new Vector3(0,.46f,0),new Vector3(.095f,goblin?.38f:.53f,.04f),new Color(.8f,.83f,.85f),false);
                }
                if(body.Kind==Kind.GoblinWarrior||body.Kind==Kind.Knight||body.Kind==Kind.Warrior)Part(rig.LeftArm,"Shield",PrimitiveType.Cube,new Vector3(0,-.16f,.09f),new Vector3(.23f,.32f,.06f),clothes,false);
            }
            parent.localScale=Vector3.one*(orc?1.12f:goblin?.85f:1);
        }
        private void AnimateFighter(Body body,PrototypeWeapon rig,Vector3 movement)
        {
            if(rig==null)return;
            bool attacking=body.PendingTarget!=0||body.AttackRecovery>0,moving=movement.sqrMagnitude>.00001f&&body.StunRemaining<=0;
            if(moving)rig.WalkCycle+=movement.magnitude*9;
            rig.Motion=body.StunRemaining>0?0:Mathf.MoveTowards(rig.Motion,moving?1:0,Time.deltaTime*(moving?16:3));
            float stride=Mathf.Sin(rig.WalkCycle)*27*rig.Motion;
            if(rig.LeftLeg!=null){rig.LeftLeg.localRotation=Quaternion.Euler(stride,0,0);rig.RightLeg.localRotation=Quaternion.Euler(-stride,0,0);}
            if(rig.Visual!=null)rig.Visual.localPosition=Vector3.up*(Mathf.Abs(Mathf.Sin(rig.WalkCycle))*.035f*rig.Motion);
            if(VillageModel.IsRanged(body.Kind))
            {
                rig.Grip.localRotation=Quaternion.identity;
                rig.RightArm.localRotation=Quaternion.Euler(attacking?-70:-stride*.5f,0,0);
                rig.LeftArm.localRotation=Quaternion.Euler(attacking?-60:stride*.5f,0,0);
            }
            else
            {
                float angle=body.PendingTarget!=0?Mathf.Lerp(-15,-105,1-Mathf.Clamp01(body.AttackWindup/.18f)):body.AttackRecovery>0?Mathf.Lerp(-105,0,1-body.AttackRecovery/.18f):0;
                rig.Grip.localRotation=Quaternion.Euler(0,-90,0)*Quaternion.AngleAxis(angle,Vector3.forward);
                rig.RightArm.localRotation=Quaternion.Euler(attacking?0:-stride*.5f,0,0);rig.LeftArm.localRotation=Quaternion.Euler(stride*.5f,0,0);
            }
        }
        private void OnContributionText(Body body,string message,FeedbackKind kind)
        {
            if(_model.Visibility(body.Position)<=0)return;
            if(_amounts.Count>=96)_amounts.RemoveAt(0);
            _amounts.Add(new FloatingAmount{Position=GroundPosition(body.Position,body.Kind==Kind.Tree?1.35f:.85f),Text=message,Kind=kind,Born=_effectTime,Offset=(body.Id%3-1)*10+(kind==FeedbackKind.Fire?32:kind==FeedbackKind.Burn?-32:0)});
        }
        private ShotView MakeShotView(int style)
        {
            var view=new ShotView{Style=style,Root=new GameObject(style>=2?"Energy projectile":"Arrow projectile")};view.Root.transform.SetParent(transform,false);
            Color color=style%2==1?new Color(.94f,.4f,.72f):_cyan;
            if(style==4)
            {
                var bolt=Line("Lightning bolt",new Color(.65f,.9f,1),.075f);bolt.transform.SetParent(view.Root.transform,false);bolt.loop=false;bolt.positionCount=9;bolt.enabled=true;view.Bolt=bolt;
            }
            else if(style>=2)
            {
                Part(view.Root.transform,"Energy shell",PrimitiveType.Sphere,Vector3.zero,new Vector3(.20f,.20f,.20f),color,false);
                Part(view.Root.transform,"Energy core",PrimitiveType.Sphere,new Vector3(0,0,.07f),new Vector3(.10f,.10f,.10f),Color.white,false);
            }
            else
            {
                Part(view.Root.transform,"Arrow shaft",PrimitiveType.Cube,Vector3.zero,new Vector3(.035f,.035f,.42f),new Color(.68f,.47f,.25f),false);
                var head=Cone(view.Root.transform,"Arrow head",new Vector3(0,0,.20f),new Vector3(.10f,.14f,.10f),new Color(.84f,.87f,.89f));head.transform.localRotation=Quaternion.Euler(90,0,0);
                Part(view.Root.transform,"Feather",PrimitiveType.Cube,new Vector3(0,0,-.17f),new Vector3(.13f,.025f,.11f),style%2==1?new Color(.85f,.35f,.24f):Color.white,false);
            }
            _shotViews.Add(view);return view;
        }
        private void UpdateContentEffects()
        {
            for(int i=_amounts.Count-1;i>=0;i--)if(_effectTime-_amounts[i].Born>=1.05f)_amounts.RemoveAt(i);
            foreach(var view in _shotViews)view.Used=false;
            foreach(var shot in _model.Projectiles)
            {
                int style=shot.Lightning?4:(shot.Magic?2:0)+(shot.Enemy?1:0);ShotView view=null;
                foreach(var item in _shotViews)if(item.Id==shot.Id){view=item;break;}
                if(view==null){foreach(var item in _shotViews)if(item.Id==0&&item.Style==style){view=item;break;}if(view==null)view=MakeShotView(style);view.Id=shot.Id;view.Previous=GroundPosition(shot.Start,shot.StartHeight);}
                view.Used=true;view.Root.SetActive(true);
                if(shot.Lightning)
                {
                    Vector3 from=GroundPosition(shot.Start,2.5f),to=GroundPosition(shot.Position,.55f),side=Vector3.Cross((to-from).normalized,Vector3.up);
                    for(int k=0;k<9;k++){float t=k/8f;Vector3 point=Vector3.Lerp(from,to,t);if(k>0&&k<8)point+=side*Mathf.Sin(k*13.1f+shot.Id*2.8f)*.22f+Vector3.up*Mathf.Sin(k*7.7f)*.12f;view.Bolt.SetPosition(k,point);}
                    view.Bolt.widthMultiplier=.035f+.08f*Mathf.Clamp01(shot.Life/.2f);continue;
                }
                Vector3 position=GroundPosition(shot.Position,shot.Height),direction=position-view.Previous;
                view.Root.transform.position=position;
                if(direction.sqrMagnitude>.00001f)view.Root.transform.rotation=Quaternion.LookRotation(direction,Vector3.up);
                view.Previous=position;view.Root.transform.localScale=Vector3.one*(shot.Magic?1+.1f*Mathf.Sin(_effectTime*18):1);
            }
            foreach(var view in _shotViews)if(!view.Used){view.Id=0;view.Root.SetActive(false);}
        }
        private void ResetContentEffects(){_amounts.Clear();foreach(var view in _shotViews){view.Id=0;view.Root.SetActive(false);}}
        private void DisposeContent(){if(_cone!=null)Destroy(_cone);}
    }
}
