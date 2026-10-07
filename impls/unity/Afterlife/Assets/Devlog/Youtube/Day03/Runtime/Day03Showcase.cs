using System.Linq;
using Afterlife.Buildings;
using Afterlife.Modules.SceneFeatures;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
namespace Afterlife.Devlog.Day03
{
    public sealed class Day03Showcase : MonoBehaviour
    {
        private const float BuildModeDelay=.8f, MenuSelectDelay=1.6f, TilePlaceDelay=3.6f;
        public Camera ShowcaseCamera;
        public SceneFeatureHost Environment;
        public Light Sun;
        public Day03Settings Settings;
        public TMP_FontAsset Font;
        public Sprite GoldHudSprite;
        public Sprite BarleyCollectionSprite;
        public Sprite WoodCollectionSprite;
        public Day03Site Farm, SecondHouse, Resource;
        public GameObject SecondHouseWindows;
        public Transform StarterHouse;
        public bool ShowGuide=true, Paused, FilmMode;
        private VillageSession _live, _comparison, _preview;
        private VillageSession _observedSession;
        private bool _skippingShot;
        private int _feedbackVersion;
        private Vector3 _workPoint;
        private Day03Ground _ground;
        private Day03Hud _hud;
        private Day03ConstructionView _construction;
        private GameObject _ownedEventSystem;
        private int _shot=-1, _harvestAtFarm;
        private float _shotTime, _lightBlend, _clickRemaining;
        private bool _automatic, _requireRelease=true, _night, _demoSelected, _menuClicked, _buildMode;
        private VillageTarget _selected=VillageTarget.None;
        private Vector2 _menuPoint, _clickPoint;
        private sealed class ShotStartState
        {
            public VillageSession Live, Comparison, Preview;
            public int HarvestAtFarm;
            public VillageTarget Selected;
            public bool DemoSelected;
        }
        private readonly System.Collections.Generic.Dictionary<int,ShotStartState> _shotStarts=new System.Collections.Generic.Dictionary<int,ShotStartState>();
        public VillageSession Current => _comparison??_preview??_live;
        public VillageSession Live => _live;
        public Vector3 Origin => transform.position;
        public float ShotTime => _shotTime;
        private void OnEnable()
        {
            if(Settings==null||ShowcaseCamera==null||Font==null||Farm==null||SecondHouse==null||Environment==null){enabled=false;return;}
            if(Resource==null)Resource=GetComponentsInChildren<Day03Site>(true).FirstOrDefault(s=>s.Target==VillageTarget.Resource);
            if(EventSystem.current==null) {
                _ownedEventSystem=new GameObject("Day03EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
                _ownedEventSystem.transform.SetParent(transform,false);
                _ownedEventSystem.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }
            var nodes=GetComponentsInChildren<Day03Site>(true).Where(s=>s.Target==VillageTarget.Resource)
                .Select(s=>Environment.transform.InverseTransformPoint(s.transform.position)).Select(p=>(p.x,p.z)).ToArray();
            _ground=new Day03Ground(Environment.Bindings.Map.Data,nodes);
            _hud=new Day03Hud(transform,Font,GoldHudSprite,BarleyCollectionSprite,WoodCollectionSprite);_hud.BuildSelected+=SelectBuilding;
            Environment.ExternalTick=true;if(Environment.Session==null)Environment.Apply();
            _construction=new Day03ConstructionView(Environment.transform,GroundHeight,Environment.Resolve().WaterHeight);
            ResetLive();
        }
        private float GroundHeight(float x,float z)=>Environment.Session?.Geometry!=null?Environment.Session.Geometry.Height(x,z):0;
        public void ResetLive()
        {
            WatchSession(null);
            _live=new VillageSession(Settings.CreateRules(),_ground);
            _comparison=null;_preview=null;_requireRelease=true;_harvestAtFarm=0;
            _selected=VillageTarget.None;_buildMode=false;_clickRemaining=0;_menuClicked=false;
            ClearFeedback();
        }
        private void ClearFeedback()
        {
            _feedbackVersion++;
            Environment?.Session?.Reset();
            _hud?.ClearTransient();
            _construction?.Clear();
        }
        private void WatchSession(VillageSession session)
        {
            if(_observedSession==session)return;
            if(_observedSession!=null){_observedSession.GoldProduced-=OnGold;_observedSession.Worked-=OnWorked;}
            _observedSession=session;
            if(_observedSession!=null){_observedSession.GoldProduced+=OnGold;_observedSession.Worked+=OnWorked;}
        }
        private void OnWorked(VillageTarget worked)
        {
            if(worked!=VillageTarget.Resource||_skippingShot)return;
            var target=Resource!=null?Resource.GetComponent<FeedbackTarget>():null;
            Vector3 point=_workPoint;
            int version=_feedbackVersion;
            System.Action feedback=()=> {
                if(version==_feedbackVersion)_hud?.ShowHarvest(WorldScreen(point));
            };
            if(Environment.Session==null||!Environment.Session.BeginSurfaceImpact(target,point,feedback)) {
                Environment.Session?.PresentSurfaceImpact(target,point);
                feedback();
            }
        }
        private void OnGold(int amount)
        {
            if(_skippingShot||FilmMode&&(_shot==0||_shot==10||_shot==12))return;
            Vector3 point=WorkPoint(VillageTarget.Farm);
            var crops=Farm.Crop!=null?Farm.Crop.GetComponentsInChildren<Renderer>(true):null;
            if(crops!=null&&crops.Length>0){
                var bounds=crops[0].bounds;foreach(var crop in crops)bounds.Encapsulate(crop.bounds);
                point=bounds.center+Vector3.up*bounds.extents.y;
            }
            _hud?.ShowProduction(WorldScreen(point),amount);
        }
        public void EnterShot(int index,bool automatic)
        {
            WatchSession(null);
            bool keepSelection=index==9&&_shot==8;
            _shot=index;_shotTime=0;_automatic=automatic;_requireRelease=true;
            if(!keepSelection){_demoSelected=false;_selected=VillageTarget.None;}
            _buildMode=false;_clickRemaining=0;_menuClicked=false;
            ClearFeedback();
            _comparison=null;_preview=null;
            if(index==0||index==2)ResetLive();
            if(index==0||index==1)_preview=ExpandedPreview();
            if(index==4||index==5){_comparison=new VillageSession(Settings.CreateRules(),_ground);_comparison.Place(VillageTarget.Farm,1,0);}
            // Shot 7-10 preserve the live placement, construction and income session.
            foreach(int later in _shotStarts.Keys.Where(i=>i>=index).ToArray())_shotStarts.Remove(later);
            _shotStarts[index]=new ShotStartState {
                Live=_live.Copy(),Comparison=_comparison?.Copy(),Preview=_preview?.Copy(),
                HarvestAtFarm=_harvestAtFarm,Selected=_selected,DemoSelected=_demoSelected
            };
        }
        public void ClearShotHistory()=>_shotStarts.Clear();
        public bool HasShotStart(int index)=>_shotStarts.ContainsKey(index);
        public void RestoreShotStart(int index,bool automatic)
        {
            var state=_shotStarts[index];
            WatchSession(null);
            _live=state.Live.Copy();
            _comparison=state.Comparison?.Copy();_preview=state.Preview?.Copy();
            _harvestAtFarm=state.HarvestAtFarm;_selected=state.Selected;_demoSelected=state.DemoSelected;
            _shot=index;_shotTime=0;_automatic=automatic;_requireRelease=true;
            _buildMode=false;_clickRemaining=0;_menuClicked=false;ClearFeedback();
            // Replaying an earlier choice replaces the subsequent take, never old future state.
            foreach(int later in _shotStarts.Keys.Where(i=>i>index).ToArray())_shotStarts.Remove(later);
        }
        private VillageSession ExpandedPreview()
        {
            var session=new VillageSession(Settings.CreateRules(),_ground);session.Place(VillageTarget.Farm,1,0);
            session.Tick(Settings.FarmConstructionSeconds+Settings.ProductionSeconds*Settings.HouseCost,VillageTarget.None,false,false);
            session.Place(VillageTarget.SecondHouse,0,-1);session.Tick(Settings.HouseConstructionSeconds,VillageTarget.None,false,false);
            return session;
        }
        public void EndFilm()
        {
            WatchSession(null);ClearFeedback();
            ClearShotHistory();
            FilmMode=false;_shot=-1;_automatic=false;_comparison=null;_preview=null;Paused=false;
            _requireRelease=true;_selected=VillageTarget.None;_buildMode=false;_clickRemaining=0;SetLightingBlend(0);
        }
        public bool ContinuousShotReady(int index)
        {
            if(index==3)return _live.FarmPurchased;
            if(index==6)return _live.FarmComplete&&_live.FarmIncome>0;
            if(index==9)return _live.SecondHouseComplete&&_live.HarvestIncome==_harvestAtFarm;
            return true;
        }
        public bool AdvanceShotForSkip(float duration)
        {
            if(!FilmMode||_live==null)return false;
            bool automatic=_automatic,paused=Paused,skipping=_skippingShot;
            _automatic=true;Paused=false;_skippingShot=true;ClearFeedback();
            // Reuse the automatic take, including real placement costs and production.
            // Bound catch-up so invalid custom placement/settings cannot hang the Editor.
            float budget=Mathf.Max(0,duration-_shotTime)+Settings.FarmConstructionSeconds+
                Settings.HouseConstructionSeconds+Settings.ProductionSeconds*(Mathf.Ceil((float)Settings.HouseCost/Settings.ProductionGold)+1)+5;
            int steps=Mathf.CeilToInt(Mathf.Min(300,budget)/.1f);
            try {
                for(int i=0;i<steps;i++) {
                    if(_shotTime>=duration&&ContinuousShotReady(_shot))return true;
                    TickScene(.1f);
                }
                Debug.LogWarning("Day03 skip could not finish the current take. Complete placement manually or restart with Home.",this);
                return false;
            } finally {_automatic=automatic;Paused=paused;_skippingShot=skipping;}
        }
        private void SelectBuilding(VillageTarget target)
        {
            if(!_buildMode||Paused)return;
            _selected=target;_requireRelease=true;
            _hud.ShowSelection();
            _menuPoint=_hud.BuildButtonScreenPoint(target);_menuClicked=true;
        }
        private bool ScreenRay(Vector2 point,out Ray ray)
        {
            if(Environment.Session!=null)return Environment.Session.ScreenRay(point,out ray);
            ray=ShowcaseCamera.ScreenPointToRay(point);return ShowcaseCamera.pixelRect.Contains(point);
        }
        private bool GroundPoint(Ray ray,out Vector3 point)
        {
            var localRay=new Ray(Environment.transform.InverseTransformPoint(ray.origin),Environment.transform.InverseTransformVector(ray.direction));
            if(Environment.Session?.Geometry!=null&&Environment.Session.Geometry.Raycast(localRay,out var local)){
                point=Environment.transform.TransformPoint(local);return true;
            }
            point=default;return false;
        }
        private Vector3 PlacementPoint(BuildingFootprint area)
        {
            float x=area.X+(area.Width-1)*.5f,z=area.Z+(area.Depth-1)*.5f;
            return Environment.transform.TransformPoint(new Vector3(x,GroundHeight(x,z),z));
        }
        private Vector3 WorkPoint(VillageTarget target)
        {
            var site=target==VillageTarget.Farm?Farm:target==VillageTarget.SecondHouse?SecondHouse:Resource;
            if(site==null)return Origin;
            // Aim at the trunk; the shared picker resolves the visible mesh surface.
            if(target==VillageTarget.Resource)return site.transform.position+Vector3.up*.55f;
            var renderers=site.Model!=null?site.Model.GetComponentsInChildren<Renderer>(true):site.GetComponentsInChildren<Renderer>(true);
            if(renderers.Length>0){var bounds=renderers[0].bounds;foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);return bounds.center;}
            var collider=site.GetComponent<BoxCollider>();
            return collider!=null?site.transform.TransformPoint(collider.center):site.transform.position;
        }
        private Vector2 WorldScreen(Vector3 world)=>Environment.Session!=null?Environment.Session.WorldToScreenPoint(world):(Vector2)ShowcaseCamera.WorldToScreenPoint(world);
        private void Update()
        {
            if(_live==null)return;
            var keys=Keyboard.current;
            if(keys!=null) {
                if(!FilmMode&&keys.hKey.wasPressedThisFrame)ShowGuide=!ShowGuide;
                if(!FilmMode&&keys.rKey.wasPressedThisFrame)ResetLive();
                if(!FilmMode&&keys.spaceKey.wasPressedThisFrame)Paused=!Paused;
                if(!FilmMode&&keys.escapeKey.wasPressedThisFrame)_selected=VillageTarget.None;
                if(!FilmMode&&keys.nKey.wasPressedThisFrame){_night=!_night;SetLightingBlend(_night?1:0);}
            }
            TickScene(Paused?0:Time.unscaledDeltaTime);
        }
        private void TickScene(float delta)
        {
            var keys=Keyboard.current;
            _shotTime+=delta;
            Environment.Session?.Tick(delta,Paused);
            bool interactive=!FilmMode||_shot==2||_shot==3||_shot==5||_shot==8||_shot==9;
            var mouse=Mouse.current;bool focused=Application.isFocused;
            bool modifier=keys!=null&&(keys.ctrlKey.isPressed||keys.shiftKey.isPressed);
            bool demoTerritory=_automatic&&_shot==9&&Current.SecondHouseComplete;
            bool demoBuild=_automatic&&_shotTime>=BuildModeDelay&&(_shot==3&&!Current.FarmPurchased||
                (_shot==8||_shot==9)&&!Current.SecondHousePurchased||demoTerritory);
            bool previousMode=_buildMode;
            if(!Paused)_buildMode=interactive&&(_automatic?demoBuild:modifier&&focused);
            if(previousMode!=_buildMode)_requireRelease=true;
            _hud.PresentBuild(Current,Settings,_buildMode,Paused||_automatic,_selected,null,false,Paused);
            VillageTarget target=VillageTarget.None;
            bool held=false,pressed=false,groundValid=false;int tileX=0,tileZ=0;
            Vector2 pointer=mouse!=null?mouse.position.ReadValue():Vector2.zero;
            if(!Paused&&focused&&mouse!=null&&interactive&&!_automatic) {
                bool blocked=_hud.BlocksWorldInput(pointer);
                if(blocked)_requireRelease=true;
                if(!mouse.leftButton.isPressed)_requireRelease=false;
                if(!blocked&&ScreenRay(pointer,out var ray)) {
                    if(_buildMode&&GroundPoint(ray,out var ground)){
                        var local=Environment.transform.InverseTransformPoint(ground);
                        tileX=Mathf.RoundToInt(local.x);tileZ=Mathf.RoundToInt(local.z);groundValid=true;
                    } else if(!_buildMode){
                        bool treeHit=Environment.Session!=null&&Environment.Session.Pointer(ray,false,false,out var picked,out _workPoint)
                            &&picked!=null&&picked.GetComponent<Day03Site>()==Resource;
                        if(treeHit)target=VillageTarget.Resource;
                        else if(Physics.Raycast(ray,out var hit,100)) {
                            var site=hit.collider.GetComponentInParent<Day03Site>();
                            if(site!=null&&(site.Target!=VillageTarget.Resource||site.GetComponent<FeedbackTarget>()==null)){
                                target=site.Target;_workPoint=hit.point;
                            }
                        }
                    }
                    held=!_requireRelease&&mouse.leftButton.isPressed;
                    pressed=!_requireRelease&&mouse.leftButton.wasPressedThisFrame;
                }
            }
            if(_automatic&&!Paused) {
                if(demoTerritory) {
                    _selected=VillageTarget.None;
                } else if(demoBuild) {
                    var kind=_shot==3?VillageTarget.Farm:VillageTarget.SecondHouse;
                    if(!_demoSelected&&_shotTime>=MenuSelectDelay){SelectBuilding(kind);_demoSelected=true;}
                    tileX=_shot==3?1:0;tileZ=_shot==3?0:-1;groundValid=true;
                    pointer=WorldScreen(PlacementPoint(Current.Footprint(kind,tileX,tileZ)));
                    pressed=_shot!=8&&_demoSelected&&_shotTime>=TilePlaceDelay&&Current.PlacementError(kind,tileX,tileZ)==null;
                } else {
                    if(_shot==2){target=VillageTarget.Resource;held=Current.HarvestIncome<Settings.HarvestLimit;}
                    if(_shot==5){target=VillageTarget.Farm;held=!Current.FarmComplete;}
                    if(_shot==9&&Current.SecondHousePurchased){target=VillageTarget.SecondHouse;held=!Current.SecondHouseComplete;}
                    pointer=WorldScreen(WorkPoint(target));
                    _workPoint=WorkPoint(target);
                    if(target==VillageTarget.Resource&&ScreenRay(pointer,out var workRay)&&Environment.Session!=null
                        &&Environment.Session.Pointer(workRay,false,false,out var picked,out var surfacePoint)
                        &&picked!=null&&picked.GetComponent<Day03Site>()==Resource)_workPoint=surfacePoint;
                }
            }
            string error=null;BuildingFootprint? preview=null;
            bool selectedPurchased=_selected==VillageTarget.Farm?Current.FarmPurchased:_selected==VillageTarget.SecondHouse&&Current.SecondHousePurchased;
            if(_buildMode&&groundValid&&_selected!=VillageTarget.None&&!selectedPurchased) {
                preview=Current.Footprint(_selected,tileX,tileZ);error=Current.PlacementError(_selected,tileX,tileZ);
                if(pressed&&!Paused&&error==null){
                    bool farmWasPurchased=Current.FarmPurchased;
                    if(Current.Place(_selected,tileX,tileZ)){
                        if(!farmWasPurchased&&Current==_live)_harvestAtFarm=_live.HarvestIncome;
                        if(!_skippingShot)_hud.ShowPlacement(_selected,_selected==VillageTarget.Farm?Settings.FarmCost:Settings.HouseCost);
                    }
                }
            }
            bool comparison=_comparison!=null;
            float simulationDelta=comparison?Mathf.Min(delta,Mathf.Max(0,8-(_shotTime-delta))):delta;
            bool frozen=FilmMode&&_shot==10&&_shotTime<4;
            bool workHeld=held&&!_buildMode&&(target==VillageTarget.Resource||!FilmMode||_shot==5||_shot==9);
            WatchSession(Current);
            bool houseWasComplete=Current.SecondHouseComplete;
            if(!frozen)Current.Tick(simulationDelta,target,workHeld,(!FilmMode||_shot==2)&&!comparison);
            if(!houseWasComplete&&Current.SecondHouseComplete&&!_skippingShot)_hud.ShowHouseCompletion();
            bool before=FilmMode&&_shot==10&&_shotTime<4;
            if(Current.FarmPurchased)Farm.transform.position=PlacementPoint(Current.FarmFootprint);
            if(Current.SecondHousePurchased)SecondHouse.transform.position=PlacementPoint(Current.HouseFootprint);
            Farm.Present(!before&&Current.FarmPurchased,Current.FarmProgress,Current.ProductionProgress);
            SecondHouse.Present(!before&&Current.SecondHousePurchased,Current.HouseProgress,0);
            if(SecondHouseWindows!=null)SecondHouseWindows.SetActive(!before&&Current.SecondHouseComplete&&_lightBlend>.4f);
            string label="";
            if(FilmMode&&_shot==10)label=before?"<b><size=32>처음 모습</size></b>\n집 1채":"<b><size=32>확장 후</size></b>\n집 2채와 농장";
            _hud.Tick(Current,Settings,delta,ShowGuide&&!FilmMode,FilmMode&&(_shot==0||_shot==12),label,_preview!=null,FilmMode&&_shot==10);
            _hud.PresentBuild(Current,Settings,_buildMode,Paused||_automatic,_selected,error,preview.HasValue,Paused);
            if(!Paused)_construction.Present(Current,_buildMode,preview,error==null);
            if(_automatic&&!demoBuild)pointer=WorldScreen(target==VillageTarget.Resource?_workPoint:WorkPoint(target));
            if(!Paused) {
                _clickRemaining=Mathf.Max(0,_clickRemaining-delta);
                bool click=pressed||_menuClicked;
                if(click){_clickPoint=_menuClicked?_menuPoint:pointer;_clickRemaining=.22f;}
                _hud.PresentInput(delta,interactive,_clickRemaining>0?_clickPoint:pointer,_buildMode||_clickRemaining>0,click,workHeld);
                _menuClicked=false;
            }
        }
        public void SetLightingBlend(float blend){_lightBlend=Mathf.Clamp01(blend);Environment?.Session?.SetLightingBlend(_lightBlend);}
        private void OnDisable()
        {
            ClearShotHistory();
            WatchSession(null);
            if(_hud!=null)_hud.BuildSelected-=SelectBuilding;
            _hud?.Dispose();_hud=null;_construction?.Dispose();_construction=null;
            if(_ownedEventSystem!=null)Destroy(_ownedEventSystem);_ownedEventSystem=null;
            Environment?.Clear();
        }
    }
}
