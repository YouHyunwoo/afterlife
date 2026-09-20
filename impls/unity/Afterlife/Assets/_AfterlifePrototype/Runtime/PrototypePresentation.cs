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
            public int SwingVariant;
            public bool ContactPending;
            public Transform ImpactPoint;
            public Quaternion SwingBasis;
            public Vector3 Approach;
            public Vector3 ClickAway;
            public Vector3 SwordGrip=new Vector3(0,.075f,0),SwordBlade=new Vector3(0,.60f,0);
            public bool Used;
            public Afterlife.Devlog.Day01.ShowcasePickSurface HitSurface;
            public Vector3 HitPosition,HitNormal,ManualLocalPoint,ManualLocalNormal;
            public Afterlife.Devlog.Day01.ShowcasePickSurface ManualSurface;
        }
        private sealed class Reaction { public bool Damage; public float Time; public Kind Kind; public Vector3 Position, Away; public Quaternion Rotation; }
        private readonly List<ToolEffect> _tools=new List<ToolEffect>();
        private readonly Dictionary<int,Reaction> _reactions=new Dictionary<int,Reaction>();
        private readonly List<int> _expiredReactions=new List<int>();
        private Texture2D _terrainTiles;
        private float _effectTime;
        private int _pendingManualId;
        private float _manualStart;
        private void BeginManualWork(Body body)
        {
            if(_pendingManualId!=0 || !_model.TryBeginManualWork(body.Id))return;
            _pendingManualId=body.Id;_manualStart=_effectTime;
            var tool=GetTool(body);
            var mouse=UnityEngine.InputSystem.Mouse.current;
            if(mouse!=null)
            {
                Vector2 screen=mouse.position.ReadValue();
                var ray=_camera.ViewportPointToRay(new Vector3(screen.x/Screen.width,screen.y/Screen.height,0));
                if(TryResourceSurface(body.Id,ray,out var surface,out float distance))
                {
                    tool.ManualSurface=surface;
                    tool.ManualLocalPoint=surface.transform.InverseTransformPoint(ray.GetPoint(distance));
                    tool.ManualLocalNormal=surface.transform.localToWorldMatrix.transpose.MultiplyVector(surface.HitNormal).normalized;
                }
            }
        }
        private void UpdateManualWork()
        {
            if(_pendingManualId==0)return;
            if(_modal!=Modal.None || !Application.isFocused){_model.CancelManualWork();_pendingManualId=0;return;}
            var body=_model.Find(_pendingManualId);
            if(body==null || !_model.CanManualWork(body.Id)){_model.CancelManualWork();_pendingManualId=0;return;}
            float age=_effectTime-_manualStart;
            if(age>=.12f){int id=_pendingManualId;_pendingManualId=0;_model.ManualWork(id);return;}
            var tool=GetTool(body);tool.Used=true;PoseTool(tool,body.Position,age/.12f);
        }

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
                if(BuildResourceTool(effect,artTool)) { }
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
                _tools.Add(effect);
            }
            effect.SwingVariant=UnityEngine.Random.Range(0,3);
            effect.ClickAway=b.Kind==Kind.Chest?ChestClickAway(b):Vector3.zero;
            effect.ManualSurface=null;
            effect.SwingBasis=_camera.transform.rotation;
            effect.Approach=_camera.transform.right*.45f+_camera.transform.up*.55f-_camera.transform.forward*.25f;
            effect.Body=b.Id;effect.Angle=_camera.transform.eulerAngles.y;effect.Impact=-100;effect.Root.SetActive(true);return effect;
        }
        private void PoseTool(ToolEffect tool,Point point,float swing)
        {
            PoseResourceTool(tool,point,Mathf.Clamp01(swing)*.12f);
        }

        private void OnContributed(Body body)
        {
            // 모델의 자원/체력 변경 이벤트와 타격 포즈를 같은 틱에 적용한다.
            var tool=GetTool(body);tool.Impact=_effectTime;tool.ContactPending=true;tool.Used=true;PoseTool(tool,body.Position,1);AddResourceMark(tool);EmitImpactChips(tool);
            Quaternion rest=_reactions.TryGetValue(body.Id,out var previousReaction)?previousReaction.Rotation:
                (_objects.TryGetValue(body.Id,out var target)?target.transform.rotation:Quaternion.identity);
            Vector3 away=Vector3.ProjectOnPlane(GroundPosition(body.Position)-tool.HitPosition,Vector3.up).normalized;
            if(away.sqrMagnitude<.001f)away=Vector3.ProjectOnPlane(-tool.HitNormal,Vector3.up).normalized;
            if(body.Kind==Kind.Chest)away=tool.ClickAway;
            if(away.sqrMagnitude<.001f)away=Vector3.forward;
            _reactions[body.Id]=new Reaction{Damage=body.Hostile||body.Kind==Kind.Animal,Time=_effectTime,Kind=body.Kind,Position=GroundPosition(body.Position),Away=away,Rotation=rest};
        }
        private void OnDamaged(Body body)
        {
            if(_reactions.TryGetValue(body.Id,out var reaction) && Mathf.Approximately(reaction.Time,_effectTime))
            {reaction.Damage=true;return;}
            _reactions[body.Id]=new Reaction{Damage=true,Time=_effectTime,Kind=body.Kind,Position=GroundPosition(body.Position),Away=-_camera.transform.forward,Rotation=Quaternion.identity};
        }
        private void OnRunEnded()
        {
            _runEndPending = true;
            ResetGoldFeedback();
            if(_model.BannerUnlocked){PlayerPrefs.SetInt("AfterlifePrototype.Unlock.TealBanner.v1",1);PlayerPrefs.Save();}
        }
        private bool ReactionAlive(int id)=>_resourceDestructions.TryGetValue(id,out var destruction)?!destruction.Effect.Completed:_reactions.TryGetValue(id,out var reaction)&&_effectTime-reaction.Time<(reaction.Kind==Kind.Chest?1.4f:(reaction.Kind==Kind.Tree||reaction.Kind==Kind.Rock)?1.25f:.38f);
        private void UpdatePresentation()
        {
            if(_modal!=Modal.Menu)_effectTime+=Time.deltaTime;
            Shader.SetGlobalVector("_CursorCircle",new Vector4(_model.WorkCenter.X,_model.WorkCenter.Z,_model.Working?_model.Radius:0,_model.PulseFraction));
            Shader.SetGlobalColor("_CursorColor",_cyan);
            Vector3 forward=_camera.transform.forward;Shader.SetGlobalFloat("_CursorAngle",Mathf.Atan2(forward.x,forward.z));
            foreach(var tool in _tools)tool.Used=false;
            UpdateManualWork();
            float until=_model.Interval*(1-_model.PulseFraction);
            if(_model.CursorAutomationUnlocked&&_model.Working&&until<.12f)
            foreach(var body in _model.Bodies)if(CanContribute(body)&&body.DistanceTo(_model.WorkCenter)<=_model.Radius)
            {
                var tool=GetTool(body);tool.Used=true;PoseTool(tool,body.Position,Mathf.Clamp01(1-until/.12f));
            }
            foreach(var tool in _tools)
            {
                if(tool.ContactPending){tool.Impact=_effectTime;tool.ContactPending=false;}
                float age=_effectTime-tool.Impact;
                if(age<.165f&&tool.Body!=0)
                {
                    tool.Used=true;var body=_model.Find(tool.Body);
                    PoseResourceTool(tool,body!=null?body.Position:new Point(tool.HitPosition.x,tool.HitPosition.z),.12f+Mathf.Max(0,age));
                }
                if(!tool.Used){tool.Body=0;tool.Root.SetActive(false);}
            }
            UpdateImpactChips();
            _expiredReactions.Clear();
            foreach(var pair in _reactions)
            {
                if(_resourceDestructions.ContainsKey(pair.Key))continue;
                float age=_effectTime-pair.Value.Time,t=Mathf.Clamp01(age/(pair.Value.Kind==Kind.Chest?1.4f:.38f));
                bool resource=pair.Value.Kind==Kind.Tree || pair.Value.Kind==Kind.Rock;
                bool chest=pair.Value.Kind==Kind.Chest;
                if(resource)t=Mathf.Clamp01(age/1.25f);
                if(t>=1)
                {
                    if((resource||chest) && _objects.TryGetValue(pair.Key,out var restored))
                    {restored.transform.rotation=pair.Value.Rotation;ResourceFlash(pair.Key,0);}
                    ResourceFlash(pair.Key,0);
                    _expiredReactions.Add(pair.Key);continue;
                }
                if(!_objects.TryGetValue(pair.Key,out var go))continue;
                var reaction=pair.Value;var body=_model.Find(pair.Key);
                if(body==null){foreach(var collider in go.GetComponentsInChildren<Collider>())collider.enabled=false;go.transform.position=reaction.Position;}
                ResourceFlash(pair.Key,resource||chest||reaction.Damage?Afterlife.Devlog.Destruction.ShowcaseImpactTiming.Flash(age):0);
                if(resource)
                {
                    bool wood=reaction.Kind==Kind.Tree;
                    float recoil=Afterlife.Devlog.Destruction.ShowcaseImpactTiming.Recoil(age,wood);
                    go.transform.position+=reaction.Away*(wood?.04f:.085f)*recoil;
                    go.transform.rotation=Quaternion.AngleAxis(recoil*(wood?9:4),Vector3.Cross(Vector3.up,reaction.Away))*reaction.Rotation;
                    ResourceFlash(pair.Key,Afterlife.Devlog.Destruction.ShowcaseImpactTiming.Flash(age));
                }
                else if(chest)
                {
                    // 처음부터 클릭 반대쪽으로 밀린 뒤 원위치까지만 복귀한다.
                    float recoil=Mathf.SmoothStep(0,1,age/.045f)*(1-Mathf.SmoothStep(0,1,(age-.045f)/.22f));
                    go.transform.position+=reaction.Away*.14f*recoil;
                    go.transform.rotation=Quaternion.AngleAxis(recoil*6,Vector3.Cross(Vector3.up,reaction.Away))*reaction.Rotation;
                    if(body==null)OpenWorldChest(go,age);
                }
                else if(reaction.Kind==Kind.Treasure)
                {go.transform.position=reaction.Position+Vector3.up*Mathf.Sin(t*Mathf.PI)*.45f;go.transform.localScale=Vector3.one*(1-t);}
                else if(VillageModel.IsBuildingKind(reaction.Kind)&&!reaction.Damage)
                {float bounce=Mathf.Sin(t*Mathf.PI*2)*(1-t)*.13f;go.transform.localScale=Vector3.Scale(go.transform.localScale,new Vector3(1-bounce*.5f,1+bounce,1-bounce*.5f));}
                else
                {
                    Vector3 away=Vector3.ProjectOnPlane(reaction.Away,Vector3.up).normalized;
                    go.transform.position+=away*Afterlife.Devlog.Destruction.ShowcaseImpactTiming.Recoil(age,false)*.07f;
                    if(!reaction.Damage)go.transform.localScale*=1+Mathf.Sin(t*Mathf.PI)*.045f;
                }
            }
            foreach(int id in _expiredReactions)_reactions.Remove(id);
            UpdateResourceDestruction();UpdateContentEffects();UpdateGrowthEffects();UpdateVegetation();UpdateHover();UpdateResourceMarks();
        }
        private void ResetPresentation()
        {
            ResetResourceDestruction();ResetImpactChips();_pendingManualId=0;_model.CancelManualWork();ResetResourceSurfaces();ResetHover();ResetVegetation();ResetGoldFeedback();ResetContentEffects();ResetGrowthEffects();_reactions.Clear();foreach(var tool in _tools){tool.Body=0;tool.Root.SetActive(false);}
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
        private void DisposePresentation(){ResetResourceDestruction();ResetImpactChips();_impactPool?.Dispose();_impactPool=null;ResetResourceSurfaces();DisposeHover();DisposeVegetation();DisposeContent();if(_terrainTiles!=null)Destroy(_terrainTiles);Shader.SetGlobalVector("_CursorCircle",Vector4.zero);Shader.SetGlobalInt("_VisionCount",0);}
    }
}
