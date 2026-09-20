using System.Collections.Generic;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private sealed class ToolEffect
        {
            public GameObject Root;
            public Transform Pivot;
            public Kind Kind;
            public int Body;
            public float Impact=-100;
            public float Angle;
            public Vector3 AxeEdge=new Vector3(.30f,.48f,0);
            public bool Used;
        }
        private sealed class Reaction { public bool Damage; public float Time; public Kind Kind; public Vector3 Position; }
        private readonly List<ToolEffect> _tools=new List<ToolEffect>();
        private readonly Dictionary<int,Reaction> _reactions=new Dictionary<int,Reaction>();
        private readonly List<int> _expiredReactions=new List<int>();
        private Texture2D _terrainTiles;
        private float _effectTime;

        private void InitializePresentation()
        {
            UploadTerrainTiles();BuildTerrainDetails();
            InitializeVegetation();InitializeHover();
        }
        private bool CanContribute(Body b)=>b.Alive&&(b.Kind==Kind.Tree||b.Kind==Kind.Rock||b.Hostile||b.Kind==Kind.Animal||b.Kind==Kind.Chest||b.Kind==Kind.Treasure||b.Building&&(b.MovingOut||b.MoveSource!=0||!b.Complete||b.Health<b.MaxHealth&&_model.Money>0));
        private ToolEffect GetTool(Body b)
        {
            foreach(var tool in _tools)if(tool.Body==b.Id)return tool;
            ToolEffect effect=null;
            foreach(var tool in _tools)if(tool.Body==0&&tool.Kind==b.Kind){effect=tool;break;}
            if(effect==null)
            {
                effect=new ToolEffect{Kind=b.Kind,Root=new GameObject("Contribution tool "+b.Kind)};
                effect.Root.transform.SetParent(transform,false);var pivot=new GameObject("Swing pivot");pivot.transform.SetParent(effect.Root.transform,false);effect.Pivot=pivot.transform;
                var brown=new Color(.48f,.28f,.13f);var metal=new Color(.77f,.82f,.83f);
                string artTool=b.Kind==Kind.Tree?"AxeTool":b.Kind==Kind.Rock?"PickTool":b.Kind==Kind.Treasure||b.Kind==Kind.Chest?"HandTool":b.Hostile||b.Kind==Kind.Animal?"SwordTool":"HammerTool";
                if(BuildWorldConnection(effect.Pivot,artTool,Vector3.forward)) { }
                else if(b.Kind==Kind.Treasure||b.Kind==Kind.Chest)
                {
                    Part(effect.Pivot,"Palm",PrimitiveType.Cube,Vector3.zero,new Vector3(.23f,.1f,.2f),new Color(.93f,.72f,.52f),false);
                    for(int f=0;f<4;f++)Part(effect.Pivot,"Finger",PrimitiveType.Cube,new Vector3(-.085f+f*.055f,0,.14f),new Vector3(.045f,.08f,.16f),new Color(.93f,.72f,.52f),false);
                    Part(effect.Pivot,"Thumb",PrimitiveType.Cube,new Vector3(.14f,0,.02f),new Vector3(.12f,.08f,.065f),new Color(.93f,.72f,.52f),false);
                }
                else
                {
                    Part(effect.Pivot,"Handle",PrimitiveType.Cube,new Vector3(0,.26f,0),new Vector3(.065f,.55f,.065f),brown,false);
                    if(b.Kind==Kind.Tree)
                    {
                        Part(effect.Pivot,"Axe blade",PrimitiveType.Cube,new Vector3(.13f,.48f,0),new Vector3(.28f,.22f,.055f),metal,false);
                        Part(effect.Pivot,"Axe edge",PrimitiveType.Cube,new Vector3(.27f,.48f,0),new Vector3(.04f,.28f,.07f),Color.white,false);
                    }
                    else if(b.Kind==Kind.Rock)
                    {
                        var head=Part(effect.Pivot,"Pickaxe head",PrimitiveType.Cube,new Vector3(0,.5f,0),new Vector3(.5f,.085f,.09f),metal,false);
                        head.transform.localRotation=Quaternion.Euler(0,0,12);
                        Part(effect.Pivot,"Pick tip",PrimitiveType.Cube,new Vector3(.24f,.44f,0),new Vector3(.05f,.14f,.055f),metal,false);
                    }
                    else if(b.Hostile||b.Kind==Kind.Animal)
                    {
                        Part(effect.Pivot,"Sword blade",PrimitiveType.Cube,new Vector3(0,.65f,0),new Vector3(.11f,.6f,.045f),metal,false);
                        Part(effect.Pivot,"Guard",PrimitiveType.Cube,new Vector3(0,.34f,0),new Vector3(.32f,.07f,.08f),_gold,false);
                    }
                    else Part(effect.Pivot,"Hammer head",PrimitiveType.Cube,new Vector3(0,.5f,0),new Vector3(.34f,.22f,.22f),metal,false);
                }
                if(b.Kind==Kind.Tree)
                {
                    // FBX는 날/손잡이가 한 메시로 병합된다. 렌더러 이름 대신 실제 메시 경계를 변환한다.
                    var min=new Vector3(float.MaxValue,float.MaxValue,float.MaxValue);var max=-min;bool found=false;
                    foreach(var filter in effect.Pivot.GetComponentsInChildren<MeshFilter>())
                    {
                        if(filter.sharedMesh==null)continue;var bounds=filter.sharedMesh.bounds;
                        for(int corner=0;corner<8;corner++)
                        {
                            var local=bounds.center+Vector3.Scale(bounds.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1));
                            var point=effect.Pivot.InverseTransformPoint(filter.transform.TransformPoint(local));
                            min=Vector3.Min(min,point);max=Vector3.Max(max,point);found=true;
                        }
                    }
                    if(found){float tip=Mathf.Abs(min.x)>Mathf.Abs(max.x)?min.x:max.x;effect.AxeEdge=new Vector3(tip*.96f,min.y+(max.y-min.y)*.77f,(min.z+max.z)*.5f);}
                }
                _tools.Add(effect);
            }
            effect.Body=b.Id;effect.Angle=UnityEngine.Random.Range(0,360);effect.Impact=-100;effect.Root.SetActive(true);return effect;
        }
        private void PoseTool(ToolEffect tool,Point point,float swing)
        {
            // 모든 도구 모델은 손잡이 아래끝이 원점이다. 이 grip은 휘두르는 동안 고정된다.
            // 타격 때 날의 로컬 접촉점이 대상 표면에 정확히 도달하도록 grip 위치를 역산한다.
            Quaternion facing=Quaternion.Euler(0,tool.Angle,0);
            Quaternion impact,pose;Vector3 edge;float height;
            float motion=swing<=1?Mathf.SmoothStep(0,1,swing):swing;
            if(tool.Kind==Kind.Tree)
            {
                Quaternion horizontal=Quaternion.Euler(90,0,0);
                // FBX 전방 보정 후 날이 -X에 있을 수도 있다. 실제 날이 진행 방향을 향하도록 부호를 맞춘다.
                float bladeSide=tool.AxeEdge.x>=0?1:-1;
                impact=Quaternion.AngleAxis(25*bladeSide,Vector3.up)*horizontal;
                pose=Quaternion.AngleAxis(Mathf.LerpUnclamped(-80,25,motion)*bladeSide,Vector3.up)*horizontal;
                edge=tool.AxeEdge;height=.62f;
            }
            else if((tool.Kind==Kind.Treasure||tool.Kind==Kind.Chest))
            {
                tool.Root.transform.position=GroundPosition(point,.34f);tool.Root.transform.rotation=facing;
                tool.Pivot.localRotation=Quaternion.Euler(Mathf.Lerp(-65,10,Mathf.Clamp01(swing)),0,0);
                tool.Pivot.localScale=new Vector3(1,1,Mathf.Lerp(1,.45f,Mathf.Clamp01(swing)));return;
            }
            else
            {
                // +X 날 방향으로 내려벤다. X축 회전(칼의 평면 밖 회전)을 사용하지 않는다.
                impact=Quaternion.AngleAxis(-115,Vector3.forward);
                pose=Quaternion.AngleAxis(Mathf.LerpUnclamped(-10,-115,motion),Vector3.forward);
                edge=(VillageModel.IsHostile(tool.Kind)||tool.Kind==Kind.Animal)?new Vector3(.055f,.82f,0):tool.Kind==Kind.Rock?new Vector3(.265f,.40f,0):new Vector3(.17f,.48f,0);
                height=tool.Kind==Kind.Rock?.42f:(VillageModel.IsHostile(tool.Kind)||tool.Kind==Kind.Animal)?.3f:.85f;
            }
            Vector3 strikeOffset=facing*(impact*edge);
            Vector3 approach=new Vector3(strikeOffset.x,0,strikeOffset.z).normalized;
            float surface=tool.Kind==Kind.Tree?.10f:tool.Kind==Kind.Rock?.25f:(VillageModel.IsHostile(tool.Kind)||tool.Kind==Kind.Animal)?.24f:.42f;
            Vector3 contact=GroundPosition(point,height)-approach*surface;
            tool.Root.transform.rotation=facing;
            tool.Root.transform.position=contact-facing*(impact*edge);
            tool.Pivot.localPosition=Vector3.zero;tool.Pivot.localScale=Vector3.one;tool.Pivot.localRotation=pose;
        }

        private void OnContributed(Body body)
        {
            // 모델의 자원/체력 변경 이벤트와 타격 포즈를 같은 틱에 적용한다.
            var tool=GetTool(body);tool.Impact=_effectTime;tool.Used=true;PoseTool(tool,body.Position,1);
            _reactions[body.Id]=new Reaction{Time=_effectTime,Kind=body.Kind,Position=GroundPosition(body.Position)};
        }
        private void OnDamaged(Body body)
        {
            _reactions[body.Id]=new Reaction{Damage=true,Time=_effectTime,Kind=body.Kind,Position=GroundPosition(body.Position)};
        }
        private void OnRunEnded()
        {
            _runEndPending = true;
            ResetGoldFeedback();
            if(_model.BannerUnlocked){PlayerPrefs.SetInt("AfterlifePrototype.Unlock.TealBanner.v1",1);PlayerPrefs.Save();}
        }
        private bool ReactionAlive(int id)=>_reactions.TryGetValue(id,out var reaction)&&_effectTime-reaction.Time<(reaction.Kind==Kind.Chest?1.4f:.38f);
        private void UpdatePresentation()
        {
            if(_modal!=Modal.Menu)_effectTime+=Time.deltaTime;
            Shader.SetGlobalVector("_CursorCircle",new Vector4(_model.WorkCenter.X,_model.WorkCenter.Z,_model.Working?_model.Radius:0,_model.PulseFraction));
            Shader.SetGlobalColor("_CursorColor",_cyan);
            Vector3 forward=_camera.transform.forward;Shader.SetGlobalFloat("_CursorAngle",Mathf.Atan2(forward.x,forward.z));
            foreach(var tool in _tools)tool.Used=false;
            float until=_model.Interval*(1-_model.PulseFraction);
            if(_model.Working&&until<.32f)
            foreach(var body in _model.Bodies)if(CanContribute(body)&&body.DistanceTo(_model.WorkCenter)<=_model.Radius)
            {
                var tool=GetTool(body);tool.Used=true;PoseTool(tool,body.Position,Mathf.Clamp01(1-until/.32f));
            }
            foreach(var tool in _tools)
            {
                float age=_effectTime-tool.Impact;
                if(age<.22f&&tool.Body!=0){tool.Used=true;var body=_model.Find(tool.Body);if(body!=null)PoseTool(tool,body.Position,1+Mathf.Clamp01((age-.035f)/.185f)*.16f);}
                if(!tool.Used){tool.Body=0;tool.Root.SetActive(false);}
            }
            _expiredReactions.Clear();
            foreach(var pair in _reactions)
            {
                float age=_effectTime-pair.Value.Time,t=Mathf.Clamp01(age/(pair.Value.Kind==Kind.Chest?1.4f:.38f));
                if(t>=1){_expiredReactions.Add(pair.Key);continue;}
                if(!_objects.TryGetValue(pair.Key,out var go))continue;
                var reaction=pair.Value;var body=_model.Find(pair.Key);
                if(body==null){foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=false;go.transform.position=reaction.Position;}
                if(reaction.Kind==Kind.Chest&&body==null)
                {OpenWorldChest(go,age);}
                else if(reaction.Kind==Kind.Treasure)
                {go.transform.position=reaction.Position+Vector3.up*Mathf.Sin(t*Mathf.PI)*.45f;go.transform.localScale=Vector3.one*(1-t);}
                else if(VillageModel.IsBuildingKind(reaction.Kind)&&!reaction.Damage)
                {float bounce=Mathf.Sin(t*Mathf.PI*2)*(1-t)*.13f;go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(1-bounce*.5f,1+bounce,1-bounce*.5f));}
                else go.transform.position+=new Vector3(Mathf.Sin(t*35),0,Mathf.Cos(t*29))*(1-t)*.065f;
            }
            foreach(int id in _expiredReactions)_reactions.Remove(id);
            UpdateContentEffects();UpdateGrowthEffects();UpdateVegetation();UpdateHover();
        }
        private void ResetPresentation()
        {
            ResetHover();ResetVegetation();ResetGoldFeedback();ResetContentEffects();ResetGrowthEffects();_reactions.Clear();foreach(var tool in _tools){tool.Body=0;tool.Root.SetActive(false);}
            _renderCache.Clear();_lightCache.Clear();_modelPositions.Clear();_displayPositions.Clear();_fogTerritoryRevision=-1;
        }
        private void UploadTerrainTiles()
        {
            var map=_model.Terrain;
            _terrainTiles=new Texture2D(map.Size,map.Size,TextureFormat.RGBA32,false,true){name="Authoritative terrain tile IDs",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            var pixels=new Color32[map.Size*map.Size];
            for(int z=0;z<map.Size;z++)for(int x=0;x<map.Size;x++)pixels[z*map.Size+x]=new Color32((byte)map.At(x-map.Radius,z-map.Radius),0,0,255);
            _terrainTiles.SetPixels32(pixels);_terrainTiles.Apply(false,true);
            Shader.SetGlobalTexture("_TerrainTiles",_terrainTiles);Shader.SetGlobalFloat("_TerrainSize",map.Size);Shader.SetGlobalFloat("_TerrainRadius",map.Radius);
        }
        private void DisposePresentation(){DisposeHover();DisposeVegetation();DisposeContent();if(_terrainTiles!=null)Destroy(_terrainTiles);Shader.SetGlobalVector("_CursorCircle",Vector4.zero);Shader.SetGlobalInt("_VisionCount",0);}
    }
}
