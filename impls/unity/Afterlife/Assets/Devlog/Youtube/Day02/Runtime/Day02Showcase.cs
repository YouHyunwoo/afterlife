using System.Collections.Generic;
using Afterlife.Modules.SceneFeatures;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Afterlife.Devlog.Day02
{
    public sealed class Day02Showcase : MonoBehaviour
    {
        public Camera ShowcaseCamera;
        public Transform[] Stages;
        public TMP_FontAsset Font;
        public Material RingMaterial;
        public UniversalRenderPipelineAsset Pipeline;
        public int SceneVersion=3;
        public bool FilmMode, Interaction=true, Paused, ShowGuide=true;
        public int StageIndex {get;private set;}
        public Vector3 Origin=>Stages[StageIndex].position;
        public bool ShowMoney=>StageIndex==3 && Interaction;
        private HarvestSession _session;
        private SceneFeatureHost _features;
        private Afterlife.Modules.WorldAudio.WorldAudioPlayer _sound;
        private bool _listenerPause;
        private Day02Resource[] _resources;
        private readonly List<LineRenderer> _rings=new List<LineRenderer>();
        private LineRenderer _preview;
        private GameObject _canvas;
        private TextMeshProUGUI _money,_guide,_feedback;
        private float _feedbackTime;
        private RenderPipelineAsset _previousPipeline,_previousQuality;
        private Vector3 _pan;
        private float _zoom=7;
        private bool _presentingWork,_night,_vision;
        private void OnEnable()
        {
            if(Stages==null||Stages.Length!=4||ShowcaseCamera==null){enabled=false;return;}
            _listenerPause=AudioListener.pause;
            var bank=Resources.Load<Afterlife.Modules.WorldAudio.WorldAudioBank>("WorldAudio/WorldAudioBank");
            _sound=new Afterlife.Modules.WorldAudio.WorldAudioPlayer(transform,bank,bank!=null?bank.WaterRipple:null);
            _previousPipeline=GraphicsSettings.defaultRenderPipeline;_previousQuality=QualitySettings.renderPipeline;
            GraphicsSettings.defaultRenderPipeline=Pipeline;QualitySettings.renderPipeline=Pipeline;
            _canvas=new GameObject("Day02Hud",typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler));_canvas.transform.SetParent(transform,false);
            var canvas=_canvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
            var scaler=_canvas.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);
            _money=Label("Gold",new Vector2(.77f,.87f),new Vector2(.96f,.96f),38);
            _money.color=new Color(1,.83f,.35f);
            _guide=Label("Guide",new Vector2(.1f,.18f),new Vector2(.9f,.25f),24);
            _feedback=Label("HarvestFeedback",new Vector2(.35f,.7f),new Vector2(.65f,.78f),30);
            _preview=Ring("FieldPreview");SetStage(0);
        }
        public void SetStage(int index)
        {
            if(_features!=null && _features.Session!=null)_features.Contact-=FeatureContact;
            _features?.Clear();
            StageIndex=Mathf.Clamp(index,0,3);_sound?.Reset();
            for(int i=0;i<Stages.Length;i++)Stages[i].gameObject.SetActive(i==StageIndex);
            _resources=Stages[StageIndex].GetComponentsInChildren<Day02Resource>(true);
            foreach(var resource in _resources)resource.ResetResource();
            foreach(var ring in _rings)if(ring!=null)Destroy(ring.gameObject);_rings.Clear();
            var nodes=_resources.Select(r=>new HarvestNode{X=r.transform.position.x,Z=r.transform.position.z}).ToArray();
            _session=new HarvestSession(nodes,StageIndex==0?HarvestMode.Click:StageIndex==1?HarvestMode.Hover:HarvestMode.Field);
            _session.Worked+=Worked;
            _features=Stages[StageIndex].GetComponent<SceneFeatureHost>();
            if(_features!=null) {
                _features.ExternalTick=true;if(_features.Session==null)_features.Apply();else _features.Session.Reset();
                _features.Contact+=FeatureContact;
                _night=false;_vision=false;_features.Session.SetNight(false,true);_features.Session.SetVision(false);
                for(int i=0;i<_resources.Length;i++) { int node=i;var target=_resources[i].GetComponent<FeedbackTarget>();if(target!=null)target.CanReceiveHit=()=>_presentingWork || (_session!=null&&node<_session.Nodes.Length&&_session.Nodes[node].Remaining>0); }
            }
            _session.Fields.Placed+=f=>_rings.Add(Ring("SpiritField"));
            _pan=Vector3.zero;_zoom=StageIndex==3?7:3.2f;
            _feedbackTime=0;_feedback.text="";_preview.enabled=false;
            PositionCamera();
        }
        private void FeatureContact(FeedbackTarget target,Vector3 point)
        {
            var resource=target.GetComponent<Day02Resource>();int index=System.Array.IndexOf(_resources,resource);
            if(StageIndex==0 && !_presentingWork && index>=0 && !Paused && Interaction)_session.Click(index,point.x,point.z);
        }
        private void Worked(int index)
        {
            if(_features==null){_resources[index].Work();_sound?.Play(!_resources[index].Stone,false,Time.unscaledTime);}
            else if(StageIndex>0) {
                var resource=_resources[index];var target=resource.GetComponent<FeedbackTarget>();
                _presentingWork=true;
                try {
                    Vector3 hit=resource.transform.position+Vector3.up*(resource.Stone?.45f:.8f);
                    _features.Session.PresentSurfaceImpact(target,hit);
                    resource.Work();
                } finally {_presentingWork=false;}
            }
            _feedbackTime=.6f;
            _feedback.text=ShowMoney?"+5 골드":_resources[index].Stone?"석재 채집":"목재 채집";
        }
        private void Update()
        {
            if(_session==null)return;
            var keys=Keyboard.current;
            if(!FilmMode&&keys!=null){
                if(keys.digit1Key.wasPressedThisFrame)SetStage(0);
                if(keys.digit2Key.wasPressedThisFrame)SetStage(1);
                if(keys.digit3Key.wasPressedThisFrame)SetStage(2);
                if(keys.digit4Key.wasPressedThisFrame)SetStage(3);
                if(keys.rKey.wasPressedThisFrame)SetStage(StageIndex);
            }
            if(keys!=null && !Paused && _features?.Session!=null) {
                if(keys.nKey.wasPressedThisFrame){_night=!_night;_features.Session.SetNight(_night);}
                if(keys.vKey.wasPressedThisFrame){_vision=!_vision;_features.Session.SetVision(_vision);}
            }
            AudioListener.pause=Paused||_listenerPause;
            float delta=Paused?0:Time.unscaledDeltaTime;
            bool input=Interaction&&!Paused&&Application.isFocused;
            int hovered=-1;bool ground=false;Vector3 point=Vector3.zero;
            var mouse=Mouse.current;
            if(mouse!=null&&input){
                var ray=ShowcaseCamera.ScreenPointToRay(mouse.position.ReadValue());
                if(_features!=null && _features.Session!=null) {
                    bool valid=_features.Session.ScreenRay(mouse.position.ReadValue(),out var mapped);
                    ground=_features.Session.Pointer(valid?(Ray?)mapped:null,mouse.delta.ReadValue().sqrMagnitude>.01f,StageIndex==0&&mouse.leftButton.wasPressedThisFrame,out var target,out point);
                    if(target!=null)hovered=System.Array.IndexOf(_resources,target.GetComponent<Day02Resource>());
                    if(ground && StageIndex>=2) {
                        var local=Stages[StageIndex].InverseTransformPoint(point);
                        ground=_features.Session.Map!=null && _features.Session.Map.At(Mathf.FloorToInt(local.x+.5f),Mathf.FloorToInt(local.z+.5f))!=Afterlife.Modules.Terrain.TerrainBiome.Water;
                        if(ground&&mouse.leftButton.wasPressedThisFrame){
                            _features.Session.PresentGroundTap(point);
                            _session.Click(hovered,point.x,point.z);
                        }
                    }
                }
                else {
                if(Physics.Raycast(ray,out var hit,100)){
                    var resource=hit.collider.GetComponentInParent<Day02Resource>();
                    if(resource!=null){hovered=System.Array.IndexOf(_resources,resource);if(hovered>=0&&_session.Nodes[hovered].Remaining<=0)hovered=-1;}
                }
                if(new Plane(Vector3.up,new Vector3(0,.12f,0)).Raycast(ray,out float distance)){
                    point=ray.GetPoint(distance);var local=point-Origin;float radius=StageIndex==3?6:2.6f;
                    ground=local.x*local.x+local.z*local.z<radius*radius;
                }
                if(hovered>=0){point=_resources[hovered].transform.position;ground=true;}
                if(ground&&mouse.leftButton.wasPressedThisFrame)_session.Click(hovered,point.x,point.z);
                }
                if(!FilmMode&&StageIndex==3){
                    _zoom=Mathf.Clamp(_zoom-mouse.scroll.ReadValue().y*.004f,3.5f,9);
                    if(mouse.rightButton.isPressed){var move=mouse.delta.ReadValue();_pan+=new Vector3(-move.x,0,-move.y)*.008f;_pan=Vector3.ClampMagnitude(_pan,4);}
                }
            }
            if(!FilmMode)PositionCamera();
            if(_features!=null && _features.Session!=null) {
                if(!input)_features.Session.Pointer(null,false,false,out _,out _);
                _features.Session.Tick(delta,Paused);
            }
            _session.Tick(input?delta:0,hovered);
            for(int i=0;i<_resources.Length;i++) { if(_features==null)_resources[i].Tick(delta,_session.Nodes[i].Remaining<=0);else {if(StageIndex>0)_resources[i].TickTool(delta);if(_session.Nodes[i].Remaining<=0){var target=_resources[i].GetComponent<FeedbackTarget>();if(target!=null)target.Break(Vector3.forward);}} }
            for(int i=0;i<_rings.Count;i++){
                bool active=i<_session.Fields.Fields.Count;_rings[i].enabled=active;
                if(active){var field=_session.Fields.Fields[i];Circle(_rings[i],new Vector3(field.X,.18f,field.Z),field.Radius);}
            }
            _preview.enabled=input&&ground&&StageIndex>=2;
            if(_preview.enabled)Circle(_preview,new Vector3(point.x,.2f,point.z),1.1f);
            _money.gameObject.SetActive(ShowMoney);_money.text="골드  "+_session.Gold;
            _feedbackTime=Mathf.Max(0,_feedbackTime-delta);_feedback.gameObject.SetActive(_feedbackTime>0);
            _guide.gameObject.SetActive(ShowGuide);
            _guide.text=(Paused?"일시정지":StageIndex==0?"나무나 바위를 클릭 · 한 번 클릭하면 한 번 채집":StageIndex==1?"나무나 바위 위에 커서를 유지 · 떠나면 채집 중단":"나무·바위 사이 클릭 · 작은 영력 필드 설치 · 커서를 다른 곳으로 이동")+
                (FilmMode?"  |  Space 정지 · ← → 이동 · H 안내 숨기기":"  |  1–4 공간 · R 초기화 · F8 필름 · H 안내 · N 낮/밤 · V 시야");
        }
        private void PositionCamera(){var offset=new Vector3(7,10,-12);ShowcaseCamera.transform.SetPositionAndRotation(Origin+Vector3.up*.6f+_pan+offset,Quaternion.LookRotation(-offset));ShowcaseCamera.orthographicSize=_zoom;}
        private LineRenderer Ring(string name){var go=new GameObject(name);go.transform.SetParent(transform,false);var line=go.AddComponent<LineRenderer>();line.sharedMaterial=RingMaterial;line.positionCount=65;line.widthMultiplier=.035f;line.startColor=line.endColor=new Color(.45f,1, .85f,.85f);return line;}
        private void Circle(LineRenderer line,Vector3 center,float radius){for(int i=0;i<=64;i++){float a=i*Mathf.PI*2/64;var p=center+new Vector3(Mathf.Cos(a)*radius,0,Mathf.Sin(a)*radius);if(_features?.Session?.Geometry!=null){var local=Stages[StageIndex].InverseTransformPoint(p);p.y=Stages[StageIndex].TransformPoint(new Vector3(local.x,_features.Session.Geometry.Height(local.x,local.z),local.z)).y+.045f;}line.SetPosition(i,p);}}
        private TextMeshProUGUI Label(string name,Vector2 min,Vector2 max,float size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(_canvas.transform,false);var text=go.GetComponent<TextMeshProUGUI>();text.font=Font;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;var rect=text.rectTransform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return text;}
        private void OnDisable(){if(_features!=null){if(_features.Session!=null)_features.Contact-=FeatureContact;_features.Clear();}AudioListener.pause=_listenerPause;_sound?.Dispose();_sound=null;GraphicsSettings.defaultRenderPipeline=_previousPipeline;QualitySettings.renderPipeline=_previousQuality;if(_canvas!=null)Destroy(_canvas);if(_preview!=null)Destroy(_preview.gameObject);foreach(var line in _rings)if(line!=null)Destroy(line.gameObject);_rings.Clear();_session=null;}
    }
}
