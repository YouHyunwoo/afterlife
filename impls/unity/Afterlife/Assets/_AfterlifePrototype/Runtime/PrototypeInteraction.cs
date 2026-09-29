using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private enum Modal { None, Barracks, Research, Settlement, Menu, Altar, RebirthShop, Tower, Loot, Settings, RebirthReview }
        private Modal _modal;
        private readonly List<(Modal modal,int body)> _modalStack=new List<(Modal,int)>();
        private bool ModalPausesGameplay(Modal modal)
        {
            if(modal==Modal.None||_sceneUi==null)return false;
            var rect=_sceneUi.Item("Modal."+modal)?.Rect;
            if(rect==null)return false;
            var surface=rect.GetComponent<PrototypeModalSurface>();
            return surface!=null&&surface.PauseGameplay;
        }
        private bool IsGameplayPausedByModal()
        {
            if(ModalPausesGameplay(_modal))return true;
            foreach(var entry in _modalStack)if(ModalPausesGameplay(entry.modal))return true;
            return false;
        }
        private void PushModal(Modal modal)
        {
            if (_model.Settings.MvpSlots && (modal == Modal.RebirthShop || modal == Modal.RebirthReview)) return;
            if (_model.Settings.MvpSlots && modal == Modal.Altar) modal = Modal.Research;
            if(_modal==modal)return;
            if(_modal!=Modal.None)_modalStack.Add((_modal,_modalBody));
            _modal=modal;_modalBody=0;_pressActive=false;
        }
        private int _modalBody;
        private Texture2D _fog;
        private Color32[] _fogPixels;
        private Texture2D _buildingSight;
        private Color32[] _buildingSightPixels;
        private const int BuildingSightPixelsPerTile=4;
        private struct BuildingSightSource
        {
            public int Id,Width,Depth;
            public float X,Z,Radius;
            public bool Matches(BuildingSightSource other)=>Id==other.Id&&Width==other.Width&&Depth==other.Depth
                &&X==other.X&&Z==other.Z&&Radius==other.Radius;
        }
        private readonly List<BuildingSightSource> _buildingSightSources=new List<BuildingSightSource>();
        private bool _buildingSightReady;
        private Vector2 _pressScreen;
        private Point _pressWorld;
        private bool _pressActive, _commandDrag, _dragging;
        private int _pressBody;
        private readonly Dictionary<int,LineRenderer> _selectedRings=new Dictionary<int,LineRenderer>();
        [SerializeField] private LineRenderer _commandLine, _arrowHead;
        private Vector3 _cameraVelocity;
        [SerializeField,Range(.02f,1f)] private float _orbitDegreesPerPixel=.2f;
        private bool _orbiting;
        private Vector3 _orbitPivot;
        private Vector2 _orbitLastPointer;
        private int _fogTerritoryRevision=-1;
        private bool _fogBuildMode;
        private readonly Vector4[] _visionSources=new Vector4[128];
        private readonly Vector4[] _lamps=new Vector4[32];
        private readonly RaycastHit[] _pickHits=new RaycastHit[64];
        private readonly Dictionary<int,Renderer[]> _renderCache=new Dictionary<int,Renderer[]>();
        private readonly Dictionary<int,Light> _lightCache=new Dictionary<int,Light>();
        private int _previousHovered;
        private bool _tileHoverValid;
        private readonly Dictionary<int,PrototypeWeapon> _weapons=new Dictionary<int,PrototypeWeapon>();
        private readonly Dictionary<int,Vector3> _modelPositions=new Dictionary<int,Vector3>();
        private readonly Dictionary<int,Vector3> _displayPositions=new Dictionary<int,Vector3>();
        private readonly System.Diagnostics.Stopwatch _cpuWatch=new System.Diagnostics.Stopwatch();
        private float _modelMilliseconds,_syncMilliseconds,_frameClock,_frameMax,_lastFrameMax,_frameSum,_averageFps;
        private int _frameCount;
        private bool _showPerformance;
        private int _lastPulse;
        private float _pulseFlash;
        private int FogSize=>_settings.MapRadius*2+1;
        private int BuildingSightSize=>FogSize*BuildingSightPixelsPerTile;

        private void InitializeInteraction()
        {
            _fog=new Texture2D(FogSize,FogSize,TextureFormat.RGBA32,false,true){name="Village territory overlay",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
            _fogPixels=new Color32[FogSize*FogSize];
            _buildingSight=new Texture2D(BuildingSightSize,BuildingSightSize,TextureFormat.RGBA32,false,true)
                {name="Building sight union",filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            _buildingSightPixels=new Color32[BuildingSightSize*BuildingSightSize];
            Shader.SetGlobalTexture("_PrototypeVision",_fog);
            Shader.SetGlobalTexture("_PrototypeBuildingSight",_buildingSight);
            Shader.SetGlobalFloat("_PrototypeMapRadius",_settings.MapRadius+.5f);
            ConfigureLine(_commandLine,_gold,.08f);_commandLine.loop=false;
            ConfigureLine(_arrowHead,_gold,.08f);_arrowHead.loop=false;
        }
        private bool OverUi(Vector2 p)
        {return DebugPanelBlocks(p)||_modal!=Modal.None||_model.Ended||_sceneUi!=null&&_sceneUi.Blocks(new Vector2(p.x,Screen.height-p.y));}
        private bool MouseWorld(Vector2 screen,out Point point)
        {
            var ray=_camera.ViewportPointToRay(new Vector3(screen.x/Screen.width,screen.y/Screen.height,0));
            if(_terrainSurface!=null && _terrainSurface.Raycast(ray,out var world,_tileHover!=null && _tileHover.HasTerrainStamps ? _hoverHeightOffset : null))
            {point=new Point(world.x,world.z);return Mathf.Abs(world.x)<_settings.MapRadius&&Mathf.Abs(world.z)<_settings.MapRadius;}
            point=default;return false;
        }
        private bool CameraUiGuard(Vector2 screen)=>_sceneUi!=null&&_sceneUi.Guards(screen);
        private void UpdateInputMode()
        {
            var keyboard=Keyboard.current;
            bool build=keyboard!=null&&(keyboard.ctrlKey.isPressed||keyboard.shiftKey.isPressed)
                &&Application.isFocused&&!_model.Frozen&&!_model.Ended;
            var next=build?InputMode.Build:InputMode.Work;
            if(_model.Mode==next)return;
            _model.SetMode(next,_selectedBuildKind??Kind.House);
            if(build)_inspected=0;
            _pressActive=false;_dragging=false;_buildingDrag=false;_commandDrag=false;
        }
        private void UpdateCamera(Vector2 screen,bool blocked,bool edgeHeld=false)
        {
            
            float nx=screen.x/Screen.width,ny=screen.y/Screen.height;
            float ex=Mathf.Min(nx,1-nx),ey=Mathf.Min(ny,1-ny),edge=Mathf.Min(ex,ey);
            Vector3 desired=Vector3.zero;bool moving=!blocked&&edgeHeld&&!_orbiting&&!CameraUiGuard(screen)&&edge<_edgeFraction;
            if(moving)
            {
                float t=Mathf.Clamp01(1-edge/_edgeFraction);
                float speed=Mathf.Lerp(_settings.CameraSlowSpeed,_settings.CameraMaxSpeed,t*t)*_cameraSpeedFactor;
                Vector2 direction=new Vector2(ex<_edgeFraction?(nx<.5f?-1:1):0,ey<_edgeFraction?(ny<.5f?-1:1):0).normalized;
                Vector3 right=_camera.transform.right;right.y=0;right.Normalize();Vector3 forward=_camera.transform.forward;forward.y=0;forward.Normalize();
                desired=(right*direction.x+forward*direction.y)*speed;
            }
            _cameraVelocity=moving?Vector3.Lerp(_cameraVelocity,desired,1-Mathf.Exp(-10*Time.deltaTime)):Vector3.zero;
            Vector3 movement=_cameraVelocity*Time.deltaTime;
            if(CameraGroundCenter(out var center))
            {
                var next=center+movement;
                next.x=Mathf.Clamp(next.x,-_settings.MapRadius,_settings.MapRadius);
                next.z=Mathf.Clamp(next.z,-_settings.MapRadius,_settings.MapRadius);
                movement=next-center;
            }
            _camera.transform.position+=movement;
        }
        private void ResetCameraGroundDistance()
        {
            if (CaptureActive) return;
            float maximum = _settings.Camera != null ? _settings.Camera.CameraMaximumGroundDistance : _settings.CameraGroundDistance;
            _cameraDistance.ResetDistance(_settings.CameraGroundDistance, maximum, _model.Territory.Count, _settings.MapRadius);
        }
        private void UpdateCameraGroundDistance()
        {
            if (CaptureOwnsInput) return;
            var settings = _settings.Camera;
            float maximum = settings != null ? settings.CameraMaximumGroundDistance : _settings.CameraGroundDistance;
            int expansionTiles = settings != null ? settings.CameraExpansionTilesToMaximum : 1;
            float followSpeed = settings != null ? settings.CameraDistanceFollowSpeed : 0f;
            bool paused = _model.Frozen || IsGameplayPausedByModal() || !Application.isFocused;
            _cameraDistance.FollowTerritory(_model.Territory.Count, _settings.CameraGroundDistance, maximum,
                expansionTiles, followSpeed, paused ? 0f : Time.deltaTime, _settings.MapRadius);
        }
        private bool CameraGroundCenter(out Vector3 point)
        {
            var ray=_camera.ViewportPointToRay(new Vector3(.5f,.5f,0));
            if(new Plane(Vector3.up,Vector3.zero).Raycast(ray,out float distance)){point=ray.GetPoint(distance);return true;}
            point=default;return false;
        }
        private void UpdateCameraOrbit(Vector2 screen,bool pressed,bool held,bool blocked)
        {
            if(!held||blocked){_orbiting=false;return;}
            if(pressed)
            {
                _orbiting=CameraGroundCenter(out _orbitPivot);_orbitLastPointer=screen;
                _pressActive=false;_dragging=false;_cameraVelocity=Vector3.zero;
            }
            if(!_orbiting)return;
            float degrees=(screen.x-_orbitLastPointer.x)*_orbitDegreesPerPixel;
            _orbitLastPointer=screen;
            _camera.transform.RotateAround(_orbitPivot,Vector3.up,degrees);
        }
        private void Update()
        {
            if(!_ready)return;Resize();
            if(_gameFlow==null||!_gameFlow.IsPlaying){_tileHover?.SetPaused(true);_audio?.TickEnvironment(false,false);return;}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(Keyboard.current!=null&&Keyboard.current.f4Key.wasPressedThisFrame)_showDebugPanel=!_showDebugPanel;
#endif
            _frameClock+=Time.unscaledDeltaTime;_frameSum+=Time.unscaledDeltaTime;_frameCount++;_frameMax=Mathf.Max(_frameMax,Time.unscaledDeltaTime*1000);
            if(_frameClock>=1){_averageFps=_frameCount/Mathf.Max(.001f,_frameSum);_lastFrameMax=_frameMax;_frameClock=_frameSum=_frameMax=0;_frameCount=0;}
            if(Keyboard.current!=null&&Keyboard.current.f3Key.wasPressedThisFrame)_showPerformance=!_showPerformance;
            if(_model.Phase==Phase.Settlement&&_modal!=Modal.Settlement&&!_modalStack.Exists(entry=>entry.modal==Modal.Settlement))
            {
                if(_modal==Modal.Research||_modal==Modal.Altar)
                {
                    _modal=Modal.None;_modalBody=0;_pressActive=false;
                }
                _modalStack.RemoveAll(entry=>entry.modal==Modal.Research||entry.modal==Modal.Altar);
                PushModal(Modal.Settlement);
            }
            if(_model.Phase==Phase.Rebirth&&_modal!=Modal.RebirthShop&&!_modalStack.Exists(entry=>entry.modal==Modal.RebirthShop))PushModal(Modal.RebirthShop);
            if(_model.Ended){_modal=Modal.None;_modalStack.Clear();}
            if(_modalBody!=0&&_modal!=Modal.None&&_modal!=Modal.Settlement&&_modal!=Modal.RebirthShop&&_model.Find(_modalBody)==null)CloseModal();
            if (!CaptureOwnsInput) UpdateInputMode();
            if(!CaptureOwnsInput&&Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame)
            {if(_modal!=Modal.None)CloseModal();else if(_model.Selected.Count>0||_inspected!=0||_selectedBuildKind.HasValue){_model.Selected.Clear();_selectedBuildKind=null;RefreshBuildSelection();_inspected=0;_pressActive=false;}else if(!_model.Ended){_modal=Modal.Menu;_modalBody=0;_pressActive=false;}}
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(_qaActive){UpdateFog();Sync();UpdatePresentation();return;}
#endif
            UpdateCameraGroundDistance();
            var mouse=Mouse.current;_hovered=0;_pointerValid=false;_tileHoverValid=false;
            if(mouse!=null)
            {
                Vector2 screen=mouse.position.ReadValue(),gui=new Vector2(screen.x,Screen.height-screen.y);
                bool blocked=OverUi(gui)||_model.Frozen||!Application.isFocused||(CaptureOwnsInput&&!CaptureInputAllowed);
                if (!CaptureOwnsInput)
                {
                    UpdateCameraOrbit(screen,mouse.middleButton.wasPressedThisFrame,mouse.middleButton.isPressed,blocked);
                    UpdateCamera(screen,blocked,mouse.rightButton.isPressed&&!mouse.rightButton.wasPressedThisFrame);
                }
                bool cameraGesture=!CaptureOwnsInput&&(mouse.rightButton.isPressed||mouse.middleButton.isPressed);
                if(cameraGesture){_pressActive=false;_dragging=false;}
                _pointerValid=MouseWorld(screen,out _pointer);
                _model.UpdatePointer(_pointer,_pointerValid&&!blocked,false);
                if(!blocked&&_pointerValid)
                {
                    _tileHoverValid=!cameraGesture;
                    Physics.SyncTransforms();
                    var ray=_camera.ViewportPointToRay(new Vector3(screen.x/Screen.width,screen.y/Screen.height,0));
                    int hitCount=Physics.RaycastNonAlloc(ray,_pickHits,_camera.farClipPlane,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Collide);float nearest=float.MaxValue;
                    for(int hi=0;hi<hitCount;hi++)
                    {
                        var hit=_pickHits[hi];var pick=hit.collider.GetComponentInParent<PrototypePick>();var body=pick!=null?_model.Find(pick.Id):null;
                        if(body!=null&&_model.Visibility(body.Position)>.12f)
                        {
                            float distance=hit.distance;
                            if(_resourceSurfaces.ContainsKey(body.Id) && !TryResourceSurface(body.Id,ray,out _,out distance))continue;
                            if(distance<nearest){_hovered=body.Id;nearest=distance;}
                        }
                    }
                    if(_model.Mode==InputMode.Build)_hovered=0;
                    if(!CaptureOwnsInput&&mouse.rightButton.wasPressedThisFrame&&_model.Mode==InputMode.Work)
                        InspectBody(_hovered);
                    if(mouse.leftButton.wasPressedThisFrame&&!cameraGesture)
                    {
                        bool inVillage=_model.Territory.Contains(VillageModel.TileKey(Mathf.RoundToInt(_pointer.X),Mathf.RoundToInt(_pointer.Z)));
                        if(_model.Mode==InputMode.Work||inVillage||_selectedBuildKind==Kind.House)
                        {
                            _pressActive=true;_dragging=false;_pressScreen=screen;_pressWorld=_pointer;_pressBody=_hovered;
                            _commandDrag=_model.Mode==InputMode.Work&&_model.Selected.Contains(_hovered);
                            var pressed=_model.Find(_hovered);
                            _buildingDrag=_model.Mode==InputMode.Work&&pressed!=null&&pressed.Building&&pressed.Complete&&!pressed.MovingOut;
                            if(_commandDrag)_pressWorld=pressed.Position;
                        }
                    }
                }
                if(_pressActive&&mouse.leftButton.isPressed)
                {
                    _dragging|=Vector2.Distance(screen,_pressScreen)>8;
                    _model.UpdatePointer(_pointer,_pointerValid&&!blocked,false);
                }
                if(_pressActive&&mouse.leftButton.wasReleasedThisFrame)
                {
                    if(!blocked&&_pointerValid)ReleasePointer(screen);
                    _pressActive=false;_dragging=false;
                }
                if(blocked&&!mouse.leftButton.isPressed)_pressActive=false;
            }
            else {_orbiting=false;_cameraVelocity=Vector3.zero;_model.UpdatePointer(default,false,false);}
            if (!CaptureOwnsInput) UpdateProgressionInput();
            // 모달별 PauseGameplay 정책을 따른다.
            _simulationTime+=IsGameplayPausedByModal()||(CaptureActive&&!CaptureInputAllowed)?0:Time.deltaTime;
            _cpuWatch.Restart();UnityEngine.Profiling.Profiler.BeginSample("Prototype.ModelTick");
            while(_simulationTime>=1f/30f){_model.Tick(1f/30f);_simulationTime-=1f/30f;}
            UnityEngine.Profiling.Profiler.EndSample();_modelMilliseconds=(float)_cpuWatch.Elapsed.TotalMilliseconds;
            if(_lastPulse!=_model.PulseCount){_lastPulse=_model.PulseCount;_pulseFlash=.25f;}_pulseFlash=Mathf.Max(0,_pulseFlash-Time.deltaTime);
            UpdateFog();
            _cpuWatch.Restart();UnityEngine.Profiling.Profiler.BeginSample("Prototype.ViewSync");
            Sync(); UpdatePresentation();
            UnityEngine.Profiling.Profiler.EndSample();_syncMilliseconds=(float)_cpuWatch.Elapsed.TotalMilliseconds;
            CompletePendingRun();
        }
        private void ReleasePointer(Vector2 screen)
        {
            if (CaptureOwnsInput)
            {
                if (CaptureInputAllowed && !_dragging)
                {
                    if (_pressBody == 0) _model.PlaceDivineField(_pressWorld);
                    else if (_model.CanDivineFieldInteract(_pressBody)) _model.PlaceDivineFieldOnTarget(_pressBody);
                }
                return;
            }
            if(_model.Mode==InputMode.Build)
            {
                if(_dragging)return;
                int x=Mathf.RoundToInt(_pointer.X),z=Mathf.RoundToInt(_pointer.Z);
                if(_selectedBuildKind.HasValue)
                    _model.Place(_selectedBuildKind.Value,x,z);
                return;
            }
            if(_dragging&&_buildingDrag){_model.BeginRelocation(_pressBody,Mathf.RoundToInt(_pointer.X),Mathf.RoundToInt(_pointer.Z));return;}
            if(_dragging&&_commandDrag){_model.AttackMove(_pointer);return;}
            if(_dragging)
            {
                var rect=Rect.MinMaxRect(Mathf.Min(_pressScreen.x,screen.x),Mathf.Min(_pressScreen.y,screen.y),Mathf.Max(_pressScreen.x,screen.x),Mathf.Max(_pressScreen.y,screen.y));
                _model.Select(_model.Bodies.Where(b=>b.Friendly&&_model.Visibility(b.Position)>.12f).Where(b=>
                {var p=_camera.WorldToViewportPoint(GroundPosition(b.Position,.3f));return p.z>0&&rect.Contains(new Vector2(p.x*Screen.width,p.y*Screen.height));}).Select(b=>b.Id));return;
            }
            if(_pressBody==0)
            {
                _model.PlaceDivineField(_pressWorld);
                return;
            }
            _model.PlaceDivineFieldOnTarget(_pressBody);
        }
        private void InspectBody(int id)
        {
            var body=_model.Find(id);
            if(body==null||!body.Alive)return;
            _inspected=id;
            if(!body.Complete||body.MovingOut)return;
            _modalBody=body.Id;
            if(_model.IsProductionBuilding(body.Kind))_modal=Modal.Barracks;
            else if(VillageModel.IsTower(body.Kind))_modal=Modal.Tower;
        }
        private void CloseModal()
        {
            if(_modal!=Modal.None&&_modal!=Modal.Settlement&&_modal!=Modal.RebirthShop)PlaySound(SoundCue.Cancel);
            // 다른 모달 위에 정산이 열려도 보상 선택/정산 완료를 먼저 처리한다.
            // 먼저 스택을 복원하면 정산 상태가 남아 다음 프레임에 같은 창이 다시 열린다.
            if(_modal==Modal.Settlement)
            {
                if(_model.DailyRewardPending){_model.OpenDailyReward();return;}
                _model.CompleteSettlement();
            }
            if(_modalStack.Count>0)
            {
                var previous=_modalStack[_modalStack.Count-1];_modalStack.RemoveAt(_modalStack.Count-1);
                _modal=previous.modal;_modalBody=previous.body;_pressActive=false;return;
            }
            if(_modal==Modal.RebirthShop){_model.FinishRebirth();RebuildVillageView();return;}
            if(_modal==Modal.Altar&&_model.Phase==Phase.Settlement){_modal=Modal.Settlement;_modalBody=0;return;}
            _modal=Modal.None;_modalBody=0;_pressActive=false;
        }
        private void RebuildVillageView()
        {
            ClearVillageView();
            if(_mapRoot==null||_mapRevision!=_model.Terrain.Revision)RebuildMap();
            _camera.transform.SetPositionAndRotation(_initialCameraPosition,_initialCameraRotation);_cameraVelocity=Vector3.zero;_orbiting=false;
            ResetCameraGroundDistance();UpdateFog();
            MeasureStartup("Objects",Sync);
        }
        private void ClearVillageView()
        {
            ResetVisionBlurFocus();
            _modalStack.Clear();
            ClearDivineFieldVisuals();
            ClearSkillPresentation();
            CloseTileBuildMenu();
            foreach(var go in _objects.Values){go.SetActive(false);Destroy(go);}_objects.Clear();
            ResetPresentation();_weapons.Clear();_animals.Clear();_worldRigs.Clear();_connectionMasks.Clear();_connectionRevision=-1;_inspected=_hovered=_previousHovered=_modalBody=0;_pressActive=false;_modal=Modal.None;_lastPulse=0;_simulationTime=0;
        }
        private void UpdateBuildingSight()
        {
            int index=0;
            bool changed=!_buildingSightReady;
            foreach(var body in _model.Bodies)
            {
                if(!body.Building)continue;
                float radius=_model.BodyVisionRadius(body);
                if(radius<=0)continue;
                var source=new BuildingSightSource{Id=body.Id,X=body.Position.X,Z=body.Position.Z,
                    Width=body.Width,Depth=body.Depth,Radius=radius};
                if(index==_buildingSightSources.Count){_buildingSightSources.Add(source);changed=true;}
                else
                {
                    if(!_buildingSightSources[index].Matches(source))changed=true;
                    _buildingSightSources[index]=source;
                }
                index++;
            }
            if(index<_buildingSightSources.Count)
            {
                _buildingSightSources.RemoveRange(index,_buildingSightSources.Count-index);
                changed=true;
            }
            if(!changed)return;
            _buildingSightReady=true;

            int size=BuildingSightSize;
            float range=_settings.MapRadius+.5f;
            float step=1f/BuildingSightPixelsPerTile;
            for(int pixel=0;pixel<_buildingSightPixels.Length;pixel++)
                _buildingSightPixels[pixel]=new Color32(0,0,0,255);
            foreach(var source in _buildingSightSources)
            {
                int minX=Mathf.Max(0,Mathf.CeilToInt((source.X-source.Radius+range)/step-.5f));
                int maxX=Mathf.Min(size-1,Mathf.FloorToInt((source.X+source.Radius+range)/step-.5f));
                int minZ=Mathf.Max(0,Mathf.CeilToInt((source.Z-source.Radius+range)/step-.5f));
                int maxZ=Mathf.Min(size-1,Mathf.FloorToInt((source.Z+source.Radius+range)/step-.5f));
                for(int z=minZ;z<=maxZ;z++)
                {
                    float worldZ=-range+(z+.5f)*step;
                    for(int x=minX;x<=maxX;x++)
                    {
                        float worldX=-range+(x+.5f)*step;
                        byte strength=(byte)Mathf.RoundToInt(Afterlife.Buildings.CircularRange.SightStrength(worldX,worldZ,source.X,source.Z,source.Radius)*255);
                        int pixel=z*size+x;
                        if(strength<=_buildingSightPixels[pixel].r)continue;
                        var color=_buildingSightPixels[pixel];color.r=strength;_buildingSightPixels[pixel]=color;
                    }
                }
            }
            _buildingSight.SetPixels32(_buildingSightPixels);
            _buildingSight.Apply(false,false);
        }

        private void UpdateFog()
        {
            UnityEngine.Profiling.Profiler.BeginSample("Prototype.VisionUpload");
            UpdateBuildingSight();
            UpdateTerritorySight();
            int count=0,lamps=0;
            foreach(var body in _model.Bodies)
            {
                if(!body.Alive)continue;
                float lightRadius=_model.BodyLightRadius(body);
                float vision=_model.BodyVisionRadius(body);
                if(!body.Building&&vision>0&&count<_visionSources.Length)
                {
                    _visionSources[count++]=new Vector4(body.Position.X,body.Position.Z,vision,1);
                }
                if(lightRadius>0&&lamps<_lamps.Length)_lamps[lamps++]=new Vector4(body.Position.X,GroundLift(body.Position.X,body.Position.Z)+.8f,body.Position.Z,lightRadius);
            }
            if(_model.PointerVisible&&count<_visionSources.Length)_visionSources[count++]=new Vector4(_model.WorkCenter.X,_model.WorkCenter.Z,_settings.CursorVision,1);
            foreach(var field in _model.DivineFields.Fields)if(count<_visionSources.Length)_visionSources[count++]=new Vector4(field.X,field.Z,Mathf.Max(1.6f,field.Radius+1f),1);
            foreach(var echo in _model.Echoes)if(count<_visionSources.Length)_visionSources[count++]=new Vector4(echo.Position.X,echo.Position.Z,_settings.CursorVision,echo.Strength(_settings.VisionTrailSeconds));
            Shader.SetGlobalVectorArray("_VisionSources",_visionSources);Shader.SetGlobalInt("_VisionCount",count);
            Shader.SetGlobalVectorArray("_VillageLamps",_lamps);Shader.SetGlobalInt("_LampCount",lamps);
            // 정적 마을 영역만 변경 시 업로드. 매 프레임 16,384×전체 오브젝트 검색 제거.
            bool buildMode=_model.Mode==InputMode.Build;
            if(_fogTerritoryRevision!=_model.TerritoryRevision||_fogBuildMode!=buildMode)
            {
                _fogTerritoryRevision=_model.TerritoryRevision;
                _fogBuildMode=buildMode;
                for(int z=0;z<FogSize;z++)for(int x=0;x<FogSize;x++)
                {
                    int tx=x-_settings.MapRadius,tz=z-_settings.MapRadius;
                    _fogPixels[z*FogSize+x]=new Color32(0,(byte)(buildMode&&_model.Territory.Contains(VillageModel.TileKey(tx,tz))?255:0),0,255);
                }
                _fog.SetPixels32(_fogPixels);_fog.Apply(false,false);
            }
            Shader.SetGlobalFloat("_PrototypeNight",_model.NightBlend);
            _camera.backgroundColor=Color.Lerp(new Color(.045f,.07f,.06f),new Color(.006f,.009f,.022f),_model.NightBlend);
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void Sync()
        {
            PrepareResourceDestruction();
            foreach(int id in _objects.Keys.ToArray())if(_model.Find(id)==null&&!ReactionAlive(id)){RemoveResourceDestruction(id);RemoveSnow(id);_resourceSurfaces.Remove(id);RemoveTreeLeaves(id);Destroy(_objects[id]);_objects.Remove(id);_renderCache.Remove(id);_lightCache.Remove(id);_modelPositions.Remove(id);_displayPositions.Remove(id);_weapons.Remove(id);_animals.Remove(id);_worldRigs.Remove(id);_connectionMasks.Remove(id);}
            foreach(var b in _model.Bodies)
            {
                if(!b.Alive)continue;
                if(!_objects.TryGetValue(b.Id,out var go)){go=CreateBody(b);_objects.Add(b.Id,go);_renderCache[b.Id]=go.GetComponentsInChildren<Renderer>();_lightCache[b.Id]=go.GetComponentInChildren<Light>();_weapons[b.Id]=go.GetComponent<PrototypeWeapon>();_animals[b.Id]=go.GetComponent<PrototypeAnimalRig>();AttachResourceSurfaces(b,_renderCache[b.Id]);AttachTreeLeaves(b,_renderCache[b.Id]);AttachSnow(b,go);}
                Vector3 targetPosition=V(b.Position);
                if(!_modelPositions.TryGetValue(b.Id,out var previous))previous=targetPosition;
                Vector3 movement=targetPosition-previous;_modelPositions[b.Id]=targetPosition;
                if(b.Unit&&(b.PendingTarget!=0||b.AttackRecovery>0)&&V(b.Facing).sqrMagnitude>.00001f)go.transform.rotation=Quaternion.LookRotation(V(b.Facing).normalized,Vector3.up);
                else if(b.Unit&&movement.sqrMagnitude>.00001f)go.transform.rotation=Quaternion.LookRotation(movement.normalized,Vector3.up);
                AnimateFighter(b,_weapons[b.Id],movement);AnimateAnimal(b,_animals[b.Id],movement);
                if(b.Kind==Kind.Chest)go.transform.rotation=Quaternion.Euler(0,b.Yaw,0);
                if((b.Kind==Kind.ArrowTower||b.Kind==Kind.MagicTower)&&V(b.Facing).sqrMagnitude>.001f){var turret=go.transform.Find("Turret");if(turret!=null)turret.localRotation=Quaternion.LookRotation(V(b.Facing));}
                if(!_displayPositions.TryGetValue(b.Id,out var displayed))displayed=targetPosition;
                displayed=b.Unit&&b.StunRemaining<=0?Vector3.Lerp(displayed,targetPosition,1-Mathf.Exp(-22*Time.deltaTime)):targetPosition;
                _displayPositions[b.Id]=displayed;go.transform.position=displayed;go.transform.localScale=b.Building&&(!b.Complete||b.MovingOut)?new Vector3(1,.25f+.75f*b.Construction,1):Vector3.one;
                go.transform.position+=Vector3.up*GroundLift(displayed.x,displayed.z);
                AnimateWorldBody(b,go,movement);
                if(b.Kind==Kind.Slime&&!_worldRigs.ContainsKey(b.Id))
                {
                    float hop=b.StunRemaining>0?0:Mathf.Abs(Mathf.Sin(_effectTime*7+b.Id))*.075f;
                    go.transform.position+=Vector3.up*hop;
                    if(b.PendingTarget!=0||b.AttackRecovery>0)go.transform.localScale=new Vector3(1.12f,.8f,1.15f);
                }
                if(b.Id==_hovered||b.Id==_previousHovered)foreach(var renderer in _renderCache[b.Id])
                {var tint=renderer.GetComponent<PrototypeTint>();Tint(renderer,b.Id==_hovered?Color.Lerp(tint.Original,Color.white,.4f):tint.Original);}
                UpdateGlow(b);
                var light=_lightCache[b.Id];if(light!=null){float radius=_model.BodyLightRadius(b);light.range=Mathf.Max(.01f,radius);light.intensity=radius>0?1.1f*_model.NightBlend:0;}
            }
            RefreshConnections();_previousHovered=_hovered;
            _workRing.enabled=false;
            if(_model.PointerVisible&&_modal==Modal.None&&_model.Mode==InputMode.Work&&!_pressActive&&!_dragging&&!_tileBuildMenu&&
                _pointerValid)
            {
                bool affordable=_model.CanPlaceField&&_model.Terrain.At(_pointer)!=TerrainKind.Water;
                Tint(_workRing,affordable?new Color(.52f,.84f,1f,.55f):new Color(1f,.3f,.3f,.6f));
                var target=_model.Find(_hovered);
                Circle(_workRing,target!=null?target.Position:_pointer,_model.DivineFieldRadius);
            }
            
            _hoverRing.enabled=false;var hover=_model.Find(_hovered);
            if(_model.Mode==InputMode.Build&&_tileHoverValid&&(_selectedBuildKind==Kind.House||_model.Territory.Contains(VillageModel.TileKey(Mathf.RoundToInt(_pointer.X),Mathf.RoundToInt(_pointer.Z)))))
                Box(_hoverRing,new Point(Mathf.RoundToInt(_pointer.X),Mathf.RoundToInt(_pointer.Z)),.98f,.98f);
            else if(_model.Mode==InputMode.Work&&hover!=null){if(hover.Building)Box(_hoverRing,hover.Position,hover.Width+.08f,hover.Depth+.08f);else Circle(_hoverRing,hover.Position,.43f);}
            else if(_tileHoverValid&&_pointerValid&&!_tileBuildMenu&&_modal==Modal.None&&!_model.Ended&&!_dragging&&_model.Mode!=InputMode.Build)
            {
                // 배치와 동일한 타일 중심으로 맞추며 기존 오브젝트 호버 테두리를 재사용한다.
                Box(_hoverRing,new Point(Mathf.RoundToInt(_pointer.X),Mathf.RoundToInt(_pointer.Z)),.98f,.98f);
            }
            _towerRange.enabled=false;if(hover!=null&&VillageModel.IsTower(hover.Kind))Circle(_towerRange,hover.Position,_model.TowerRange(hover));
            _preview.enabled=false;
            if(_model.Mode==InputMode.Build&&_selectedBuildKind.HasValue&&_tileHoverValid&&_modal==Modal.None)
            {
                int x=Mathf.RoundToInt(_pointer.X),z=Mathf.RoundToInt(_pointer.Z);
                var stats=_model.Building(_selectedBuildKind.Value);
                Box(_preview,new Point(x+(stats.Width-1)*.5f,z+(stats.Depth-1)*.5f),stats.Width,stats.Depth);
                Tint(_preview,_model.PlacementError(_selectedBuildKind.Value,x,z)==null?_cyan:Color.red);
            }
            if(_pressActive&&_buildingDrag&&_dragging&&_pointerValid)
            {
                var moving=_model.Find(_pressBody);if(moving!=null){int x=Mathf.RoundToInt(_pointer.X),z=Mathf.RoundToInt(_pointer.Z);Box(_preview,new Point(x+(moving.Width-1)*.5f,z+(moving.Depth-1)*.5f),moving.Width,moving.Depth);Tint(_preview,_model.MoveError(moving,x,z)==null?_cyan:Color.red);}
            }
            _rally.enabled=false;var inspected=_model.Find(_inspected);
            if(inspected!=null&&inspected.Building){Box(_rally,inspected.Position,inspected.Width+.15f,inspected.Depth+.15f);}
            foreach(var pair in _selectedRings)pair.Value.enabled=false;
            foreach(int id in _model.Selected)
            {
                var unit=_model.Find(id);if(unit==null||_model.Visibility(unit.Position)<.12f)continue;
                if(!_selectedRings.TryGetValue(id,out var ring)){ring=Line("Selected troop",new Color(.4f,.8f,1),.05f);_selectedRings.Add(id,ring);}Circle(ring,unit.Position,.4f);
            }
            UpdateDivineFieldVisuals();
            _commandLine.enabled=_arrowHead.enabled=_pressActive&&_dragging&&_commandDrag&&_pointerValid&&_modal==Modal.None;
            if(_commandLine.enabled)
            {
                _commandLine.positionCount=2;_commandLine.SetPositions(new[]{GroundPosition(_pressWorld,.2f),GroundPosition(_pointer,.2f)});
                Vector3 end=GroundPosition(_pointer,.2f),dir=(end-GroundPosition(_pressWorld,.2f)).normalized,side=Vector3.Cross(dir,Vector3.up)*.3f;
                _arrowHead.positionCount=3;_arrowHead.SetPositions(new[]{end-dir*.6f+side,end,end-dir*.6f-side});
            }
        }
        private void OnDestroy()
        {
            DisposeTerritorySight();
            DisposeVisionBlur();
            EndCapture();
            ClearDivineFieldVisuals();
            _terrainSurface?.Dispose();_terrainSurface=null;
            _waterSurface?.Dispose();_waterSurface=null;
            DisposeGameFlow();
            ClearSkillPresentation();
            if(_model!=null)_model.LightningStruck-=ShowSkillLightning;
            if(_audio!=null)_audio.enabled=false;
            DisposeBuildItems();
            if(_model!=null)_model.RelicsChanged-=SyncRelicHud;
            foreach(var item in _relicItems.Values)if(item!=null)Destroy(item.gameObject);
            _relicItems.Clear();
            if(_model!=null){_model.ArrowRayBlocked=null;_model.Notice-=OnNotice;_model.Contributed-=OnContributed;_model.Damaged-=OnDamaged;_model.ContributionText-=OnContributionText;_model.HarvestGold-=OnHarvestGold;_model.RunEnded-=OnRunEnded;}
            DisposeBuildTileMenu();DisposePresentation();DisposeWorldArt();
            if(_pixels!=null){_pixels.Release();Destroy(_pixels);}if(_fog!=null)Destroy(_fog);if(_buildingSight!=null)Destroy(_buildingSight);
            foreach(var material in _materials)if(material!=null)Destroy(material);
            DisposeUi();foreach(var roof in _roofs.Values)Destroy(roof);if(_font!=null&&_font!=Resources.Load<Font>("Fonts/Mona12TextKR"))Destroy(_font);
            Shader.SetGlobalTexture("_PrototypeVision",Texture2D.whiteTexture);
            Shader.SetGlobalTexture("_PrototypeBuildingSight",Texture2D.blackTexture);
            Shader.SetGlobalFloat("_PrototypeNight",0);
        }
    }
}
