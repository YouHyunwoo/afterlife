using System.Collections.Generic;
using Afterlife.Modules.SceneFeatures;
using Afterlife.Modules.HarvestFeedback;
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
        public Sprite WoodCollectionSprite,StoneCollectionSprite;
        public Sprite GoldHudSprite;
        [Header("Collection feedback")]
        [Min(1)] public float CollectionIconSize=48,CollectionSpread=96,CollectionArc=120;
        public Material RingMaterial;
        public UniversalRenderPipelineAsset Pipeline;
        public int SceneVersion=4;
        public bool Demonstration;
        public int DemoWorkLimit;
        public int DemoTreeWorks;
        public bool DemoFixedTarget;
        public float DemoStart;
        public int WorkAmount=1;
        private bool _requireRelease=true;
        private float _demoTime;
        private Vector3 _workPoint;
        private Vector2 _workScreenPoint;
        private int _pendingWorkIndex=-1,_displayGold;
        public bool FilmMode, Interaction=true, Paused, ShowGuide;
        public int StageIndex {get;private set;}
        public Vector3 Origin=>Stages[StageIndex].position;
        public bool ShowMoney=>StageIndex==3 && Interaction;
        private HarvestSession _session;
        private SceneFeatureHost _features;
        private Afterlife.Modules.WorldAudio.WorldAudioPlayer _sound;
        private bool _listenerPause;
        private Day02Resource[] _resources;
        
        
        private GameObject _canvas;
        private GameObject _goldHud;
        private TextMeshProUGUI _money,_guide,_comparison;
        private HarvestFeedbackPresenter _harvestFeedback;
        private PointerInputPresenter _inputFeedback;
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
            var goldIcon=CreateGoldHud();
            _guide=Label("Guide",new Vector2(.1f,.18f),new Vector2(.9f,.25f),24);
            _guide.gameObject.SetActive(ShowGuide);
            _comparison=Label("EfficiencyComparison",new Vector2(.2f,.8f),new Vector2(.8f,.87f),28);
            _harvestFeedback=_canvas.AddComponent<HarvestFeedbackPresenter>();
            _harvestFeedback.Initialize(Font,goldIcon,WoodCollectionSprite,StoneCollectionSprite);
            _harvestFeedback.IconSize=CollectionIconSize;
            _harvestFeedback.Spread=CollectionSpread;
            _harvestFeedback.Arc=CollectionArc;
            _harvestFeedback.IconArrived+=OnIconArrived;
            _inputFeedback=_canvas.AddComponent<PointerInputPresenter>();_inputFeedback.Initialize(Font);
            SetStage(0);
        }
        public void SetStage(int index)
        {
            if(_session!=null)_session.Worked-=Worked;
            _pendingWorkIndex=-1;_displayGold=0;_harvestFeedback?.Clear();_inputFeedback?.Clear();
            DemoWorkLimit=0;DemoFixedTarget=false;DemoStart=0;DemoTreeWorks=0;
            
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
                for(int i=0;i<_resources.Length;i++) { int node=i;var target=_resources[i].GetComponent<FeedbackTarget>();if(target!=null)target.CanReceiveHit=()=>_presentingWork || _pendingWorkIndex==node || (_session!=null&&node<_session.Nodes.Length&&_session.Nodes[node].Remaining>0); }
            }
            WorkAmount=StageIndex>=2?3:1;_demoTime=0;_requireRelease=true;
            _pan=Vector3.zero;_zoom=3.8f;
            PositionCamera();
        }
        private void Worked(int index)
        {
            var resource=_resources[index];int amount=_session.LastAmount;
            Vector2 screenPoint=_workScreenPoint;bool money=ShowMoney;
            System.Action feedback=()=> {
                _pendingWorkIndex=-1;
                if(money)_harvestFeedback.ShowCollection(screenPoint,resource.Stone,amount);
                else _harvestFeedback.ShowText(screenPoint,(resource.Stone?"석재":"목재")+" +"+amount,
                    resource.Stone?new Color(.9f,.94f,1):new Color(1,.87f,.53f));
            };
            if(_features==null){resource.Work();_sound?.Play(!resource.Stone,false,Time.unscaledTime);feedback();}
            else {
                var target=resource.GetComponent<FeedbackTarget>();
                _pendingWorkIndex=index;
                _presentingWork=true;
                try {
                    if(!_features.Session.BeginSurfaceImpact(target,_workPoint,feedback)) {
                        _features.Session.PresentSurfaceImpact(target,_workPoint);feedback();
                    }
                } finally {_presentingWork=false;}
            }
        }
        private void OnIconArrived(){_displayGold++;}
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
            bool ready=_features?.Session==null||!_features.Session.IsSwinging;
            if(input&&Demonstration) {
                float previous=_demoTime;_demoTime+=delta;
                float age=Mathf.Max(0,_demoTime-DemoStart);
                if(DemoTreeWorks>0) {
                    var demo=HarvestDemoSequence.Evaluate(_session.WorkCount,DemoTreeWorks,DemoWorkLimit,
                        _demoTime>=DemoStart,StageIndex==0,ready&&_session.IsReady);
                    for(int i=0;i<_resources.Length;i++)
                        if(_resources[i].Stone==demo.Stone&&_session.Nodes[i].Remaining>0){hovered=i;break;}
                    held=hovered>=0&&demo.Held;pressed=hovered>=0&&demo.Pressed;
                } else {
                    int candidate=DemoFixedTarget?System.Array.FindIndex(_resources,r=>!r.Stone):Mathf.FloorToInt(age/4)%_resources.Length;
                    if(candidate<0)candidate=0;
                    for(int n=0;n<_resources.Length;n++){int i=(candidate+n)%_resources.Length;if(_session.Nodes[i].Remaining>0){hovered=i;break;}}
                    held=_demoTime>=DemoStart && (DemoWorkLimit<=0||_session.WorkCount<DemoWorkLimit) && (DemoFixedTarget||age%5<4);
                    pressed=held&&(previous<=DemoStart||Mathf.FloorToInt(age/.9f)!=Mathf.FloorToInt(Mathf.Max(0,previous-DemoStart)/.9f));
                }
                if(hovered>=0)point=_resources[hovered].transform.position+Vector3.up*(_resources[hovered].Stone?.45f:.8f);
            } else if(mouse!=null&&input&&_features?.Session!=null) {
                bool valid=_features.Session.ScreenRay(mouse.position.ReadValue(),out var ray);
                bool hit=_features.Session.Pointer(valid?(Ray?)ray:null,mouse.delta.ReadValue().sqrMagnitude>.01f,false,out var target,out point);
                if(target!=null)hovered=System.Array.IndexOf(_resources,target.GetComponent<Day02Resource>());
                held=!_requireRelease&&mouse.leftButton.isPressed;
                pressed=!_requireRelease&&mouse.leftButton.wasPressedThisFrame;
                if(hit&&hovered<0&&pressed)_features.Session.PresentGroundTap(point);
            }
            _workPoint=point;_session.WorkAmount=WorkAmount;
            _workScreenPoint=Demonstration?(_features?.Session!=null?_features.Session.WorldToScreenPoint(point):(Vector2)ShowcaseCamera.WorldToScreenPoint(point)):
                (mouse!=null?mouse.position.ReadValue():Vector2.zero);
            _session.Tick(input?delta:0,hovered,input&&held&&ready,input&&pressed&&ready);
            if(!Paused)_inputFeedback.Tick(delta,input,_workScreenPoint,StageIndex==0,pressed,held);
            if(!FilmMode)PositionCamera();
            if(_features!=null && _features.Session!=null) {
                if(!input)_features.Session.Pointer(null,false,false,out _,out _);
                _features.Session.Tick(delta,Paused);
            }

            for(int i=0;i<_resources.Length;i++) { if(_features==null)_resources[i].Tick(delta,_session.Nodes[i].Remaining<=0);else {if(_session.Nodes[i].Remaining<=0&&_pendingWorkIndex!=i){var target=_resources[i].GetComponent<FeedbackTarget>();if(target!=null)target.Break(Vector3.forward);}} }
            _harvestFeedback.Tick(delta);
            _goldHud.SetActive(ShowMoney);_money.text=_displayGold.ToString("N0");
            _comparison.gameObject.SetActive(StageIndex==2&&Interaction);
            _comparison.text=(DemoFixedTarget?"같은 나무 · 0.7초 간격 · 작업 "+_session.WorkCount+"/"+DemoWorkLimit+"회":"회당 "+WorkAmount+" · 작업 "+_session.WorkCount+"회")+
                " · 총 채집 "+_session.Gold+"\n<size=20>"+(WorkAmount==1?"강화 전 · 회당 +1":"강화 후 · 회당 +3")+" · 강화 예시</size>";
            _guide.gameObject.SetActive(ShowGuide);
            _guide.text=(Paused?"일시정지":StageIndex==0?"클릭 한 번 · 채집 한 번":"왼쪽 버튼 유지 · 현재 커서 대상에 반복 작업 · 놓으면 중단")+
                (StageIndex==2?"  |  회당 채집량 ×"+WorkAmount+(!FilmMode?" · U 강화 비교 초기화":""):"")+
                (FilmMode?(Demonstration?"  |  자동 시연 미리보기":"  |  직접 조작 촬영")+" · Space 정지 · ← → 샷 · Home 재시작 · H 안내":"  |  1–4 공간 · R 초기화 · F8 촬영 · F9 자동 시연 · H 안내 · N 낮/밤 · V 시야");
        }
        private void PositionCamera(){var offset=new Vector3(7,10,-12);ShowcaseCamera.transform.SetPositionAndRotation(Origin+Vector3.up*.6f+_pan+offset,Quaternion.LookRotation(-offset));ShowcaseCamera.orthographicSize=_zoom;}
        private void OnApplicationFocus(bool focused){if(!focused)_requireRelease=true;}
        private RectTransform CreateGoldHud()
        {
            _goldHud=new GameObject("GoldHud",typeof(RectTransform));
            _goldHud.transform.SetParent(_canvas.transform,false);
            var hudRect=(RectTransform)_goldHud.transform;
            hudRect.anchorMin=new Vector2(.77f,.87f);hudRect.anchorMax=new Vector2(.96f,.96f);
            hudRect.offsetMin=hudRect.offsetMax=Vector2.zero;

            var iconObject=new GameObject("GoldIcon",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            iconObject.transform.SetParent(_goldHud.transform,false);
            var icon=iconObject.GetComponent<UnityEngine.UI.Image>();
            icon.sprite=GoldHudSprite;icon.preserveAspect=true;icon.raycastTarget=false;
            var iconRect=icon.rectTransform;
            iconRect.anchorMin=iconRect.anchorMax=new Vector2(0,.5f);
            iconRect.anchoredPosition=new Vector2(40,0);iconRect.sizeDelta=new Vector2(48,48);

            _money=Label("GoldAmount",Vector2.zero,Vector2.one,38);
            _money.transform.SetParent(_goldHud.transform,false);
            _money.rectTransform.offsetMin=new Vector2(78,0);
            _money.alignment=TextAlignmentOptions.MidlineLeft;
            _money.color=new Color(1,.83f,.35f);_money.text="0";
            _goldHud.SetActive(false);
            return iconRect;
        }
        private TextMeshProUGUI Label(string name,Vector2 min,Vector2 max,float size){var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(_canvas.transform,false);var text=go.GetComponent<TextMeshProUGUI>();text.font=Font;text.fontSize=size;text.alignment=TextAlignmentOptions.Center;text.raycastTarget=false;var rect=text.rectTransform;rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;return text;}
        private void OnDisable(){if(_session!=null)_session.Worked-=Worked;if(_harvestFeedback!=null){_harvestFeedback.IconArrived-=OnIconArrived;_harvestFeedback.Clear();}_inputFeedback?.Clear();if(_features!=null){_features.Clear();}AudioListener.pause=_listenerPause;_sound?.Dispose();_sound=null;GraphicsSettings.defaultRenderPipeline=_previousPipeline;QualitySettings.renderPipeline=_previousQuality;if(_canvas!=null)Destroy(_canvas);_harvestFeedback=null;_inputFeedback=null;_pendingWorkIndex=-1;_session=null;}
    }
}
