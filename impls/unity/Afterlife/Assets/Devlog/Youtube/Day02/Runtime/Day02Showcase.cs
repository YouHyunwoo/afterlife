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
        public int SceneVersion=4;
        public bool Demonstration;
        public int WorkAmount=1;
        private bool _requireRelease=true;
        private float _demoTime;
        private Vector3 _workPoint;
        public bool FilmMode, Interaction=true, Paused, ShowGuide=true;
        public int StageIndex {get;private set;}
        public Vector3 Origin=>Stages[StageIndex].position;
        public bool ShowMoney=>StageIndex==3 && Interaction;
        private HarvestSession _session;
        private SceneFeatureHost _features;
        private Afterlife.Modules.WorldAudio.WorldAudioPlayer _sound;
        private bool _listenerPause;
        private Day02Resource[] _resources;
        
        
        private GameObject _canvas;
        private TextMeshProUGUI _money,_guide,_feedback,_comparison;
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
            _comparison=Label("EfficiencyComparison",new Vector2(.2f,.8f),new Vector2(.8f,.87f),28);
            SetStage(0);
        }
        public void SetStage(int index)
        {
            
            _features?.Clear();
            StageIndex=Mathf.Clamp(index,0,3);_sound?.Reset();
            for(int i=0;i<Stages.Length;i++)Stages[i].gameObject.SetActive(i==StageIndex);
            _resources=Stages[StageIndex].GetComponentsInChildren<Day02Resource>(true);
            foreach(var resource in _resources)resource.ResetResource();
            
            var nodes=_resources.Select(r=>new HarvestNode{X=r.transform.position.x,Z=r.transform.position.z}).ToArray();
            _session=new HarvestSession(nodes,StageIndex==0?HarvestMode.Click:HarvestMode.Hold);
            _session.Worked+=Worked;
            _features=Stages[StageIndex].GetComponent<SceneFeatureHost>();
            if(_features!=null) {
                _features.ExternalTick=true;if(_features.Session==null)_features.Apply();else _features.Session.Reset();
                
                _night=false;_vision=false;_features.Session.SetNight(false,true);_features.Session.SetVision(false);
                for(int i=0;i<_resources.Length;i++) { int node=i;var target=_resources[i].GetComponent<FeedbackTarget>();if(target!=null)target.CanReceiveHit=()=>_presentingWork || (_session!=null&&node<_session.Nodes.Length&&_session.Nodes[node].Remaining>0); }
            }
            WorkAmount=StageIndex>=2?3:1;_demoTime=0;_requireRelease=true;
            _pan=Vector3.zero;_zoom=3.8f;
            _feedbackTime=0;_feedback.text="";
            PositionCamera();
        }
        private void Worked(int index)
        {
            if(_features==null){_resources[index].Work();_sound?.Play(!_resources[index].Stone,false,Time.unscaledTime);}
            else {
                var resource=_resources[index];var target=resource.GetComponent<FeedbackTarget>();
                _presentingWork=true;
                try {
                    Vector3 hit=_workPoint;
                    _features.Session.PresentSurfaceImpact(target,hit);
                    resource.Work();
                } finally {_presentingWork=false;}
            }
            _feedbackTime=.6f;
            _feedback.text=ShowMoney?"+"+_session.LastAmount+" 골드":(_resources[index].Stone?"석재":"목재")+" +"+_session.LastAmount;
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
                if(StageIndex==2&&keys.uKey.wasPressedThisFrame){int amount=WorkAmount==1?3:1;SetStage(2);WorkAmount=amount;}
            }
            if(keys!=null && !Paused && _features?.Session!=null) {
                if(keys.nKey.wasPressedThisFrame){_night=!_night;_features.Session.SetNight(_night);}
                if(keys.vKey.wasPressedThisFrame){_vision=!_vision;_features.Session.SetVision(_vision);}
            }
            AudioListener.pause=Paused||_listenerPause;
            float delta=Paused?0:Time.unscaledDeltaTime;
            var mouse=Mouse.current;
            bool blocked=UnityEngine.EventSystems.EventSystem.current!=null&&UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
            bool input=Interaction&&!Paused&&Application.isFocused&&!blocked;
            if(!input)_requireRelease=true;
            if(input&&mouse!=null&&!mouse.leftButton.isPressed)_requireRelease=false;
            int hovered=-1;Vector3 point=Vector3.zero;bool held=false,pressed=false;
            if(input&&Demonstration) {
                _demoTime+=delta;
                int candidate=Mathf.FloorToInt(_demoTime/4)%_resources.Length;
                for(int n=0;n<_resources.Length;n++){int i=(candidate+n)%_resources.Length;if(_session.Nodes[i].Remaining>0){hovered=i;break;}}
                if(hovered>=0)point=_resources[hovered].transform.position+Vector3.up*(_resources[hovered].Stone?.45f:.8f);
                held=_demoTime%5<4;pressed=held&&Mathf.FloorToInt(_demoTime/.9f)!=Mathf.FloorToInt((_demoTime-delta)/.9f);
            } else if(mouse!=null&&input&&_features?.Session!=null) {
                bool valid=_features.Session.ScreenRay(mouse.position.ReadValue(),out var ray);
                bool hit=_features.Session.Pointer(valid?(Ray?)ray:null,mouse.delta.ReadValue().sqrMagnitude>.01f,false,out var target,out point);
                if(target!=null)hovered=System.Array.IndexOf(_resources,target.GetComponent<Day02Resource>());
                held=!_requireRelease&&mouse.leftButton.isPressed;
                pressed=!_requireRelease&&mouse.leftButton.wasPressedThisFrame;
                if(hit&&hovered<0&&pressed)_features.Session.PresentGroundTap(point);
            }
            _workPoint=point;_session.WorkAmount=WorkAmount;
            _session.Tick(input?delta:0,hovered,input&&held,input&&pressed);
            if(!FilmMode)PositionCamera();
            if(_features!=null && _features.Session!=null) {
                if(!input)_features.Session.Pointer(null,false,false,out _,out _);
                _features.Session.Tick(delta,Paused);
            }

            for(int i=0;i<_resources.Length;i++) { if(_features==null)_resources[i].Tick(delta,_session.Nodes[i].Remaining<=0);else {_resources[i].TickTool(delta);if(_session.Nodes[i].Remaining<=0){var target=_resources[i].GetComponent<FeedbackTarget>();if(target!=null)target.Break(Vector3.forward);}} }
            _money.gameObject.SetActive(ShowMoney);_money.text="골드  "+_session.Gold;
            _comparison.gameObject.SetActive(StageIndex==2&&Interaction);
            _comparison.text="회당 "+WorkAmount+" · 작업 "+_session.WorkCount+"회 · 총 채집 "+_session.Gold+"  <size=20>촬영용 강화 예시</size>";
            _feedbackTime=Mathf.Max(0,_feedbackTime-delta);_feedback.gameObject.SetActive(_feedbackTime>0);
            _guide.gameObject.SetActive(ShowGuide);
            _guide.text=(Paused?"일시정지":StageIndex==0?"클릭 한 번 · 채집 한 번":"왼쪽 버튼 유지 · 현재 커서 대상에 반복 작업 · 놓으면 중단")+
                (StageIndex==2?"  |  회당 채집량 ×"+WorkAmount+(!FilmMode?" · U 강화 비교 초기화":""):"")+
                (FilmMode?(Demonstration?"  |  자동 시연 미리보기":"  |  직접 조작 촬영")+" · Space 정지 · ← → 샷 · Home 재시작 · H 안내":"  |  1–4 공간 · R 초기화 · F8 촬영 · F9 자동 시연 · H 안내 · N 낮/밤 · V 시야");
        }
        private void PositionCamera(){var offset=new Vector3(7,10,-12);ShowcaseCamera.transform.SetPositionAndRotation(Origin+Vector3.up*.6f+_pan+offset,Quaternion.LookRotation(-offset));ShowcaseCamera.orthographicSize=_zoom;}
        private void OnApplicationFocus(bool focused){if(!focused)_requireRelease=true;}
        private TextMeshProUGUI Label(string name,Vector2 min,Vector2 max,float size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(_canvas.transform,false);var text=go.GetComponent<TextMeshProUGUI>();text.font=Font;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;var rect=text.rectTransform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return text;}
        private void OnDisable(){if(_features!=null){_features.Clear();}AudioListener.pause=_listenerPause;_sound?.Dispose();_sound=null;GraphicsSettings.defaultRenderPipeline=_previousPipeline;QualitySettings.renderPipeline=_previousQuality;if(_canvas!=null)Destroy(_canvas);_session=null;}
    }
}
