using System;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        [SerializeField] private PrototypeCanvas _sceneUi;
        public PrototypeCanvas SceneUi { get=>_sceneUi;set=>_sceneUi=value; }
        private Modal _shownModal;private float _uiRefresh;private Vector2 _baseUiResolution;
        private int _shownModalDepth;
        private int _shownDaySequence=-1;
        private int _settingsTab;
        private Kind[] _configuredBuildKinds=BuildKinds;
        private PrototypeDayStartBanner _dayBannerAnimation;
        private void UpdateDayBanner()
        {
            var ui=_sceneUi;if(ui.DayBanner==null)return;
            if(_dayBannerAnimation==null)
            {
                _dayBannerAnimation=ui.DayBanner.GetComponent<PrototypeDayStartBanner>();
                if(_dayBannerAnimation==null)_dayBannerAnimation=ui.DayBanner.gameObject.AddComponent<PrototypeDayStartBanner>();
            }
            bool day=_model.Phase==Phase.Day;
            if(day&&_shownDaySequence!=_model.DaySequence)
            {
                _shownDaySequence=_model.DaySequence;_dayBannerAnimation.Play(_model.Wave);
            }
            if(!day&&_dayBannerAnimation.IsPlaying)_dayBannerAnimation.Hide();
        }
        private sealed class CollectionFlight
        {
            public bool Active;public Vector2 Origin,Scatter;public float Age,Delay,Spin;public int Gold;
        }
        private CollectionFlight[] _collectionFlights;
        private double _displayGold;
        private int _pendingHarvestGold,_lastGoldLabel=-1;
        private float _goldPulse;
        private Vector3 _goldTargetScale=Vector3.one;
        private void ResetGoldFeedback()
        {
            if(_collectionFlights!=null)foreach(var flight in _collectionFlights)flight.Active=false;
            if(_sceneUi!=null&&_sceneUi.CollectionIcons!=null)foreach(var icon in _sceneUi.CollectionIcons)if(icon!=null)icon.gameObject.SetActive(false);
            _pendingHarvestGold=0;_displayGold=_model!=null?_model.Money:0;_lastGoldLabel=-1;_goldPulse=0;
            if(_sceneUi!=null&&_sceneUi.GoldTarget!=null)_sceneUi.GoldTarget.localScale=_goldTargetScale;
        }
        private void OnHarvestGold(Point point,Kind kind,int gold)
        {
            var ui=_sceneUi;
            if(ui==null||ui.CollectionLayer==null||ui.GoldTarget==null||_collectionFlights==null||gold<=0)return;
            var view=_camera.WorldToViewportPoint(GroundPosition(point,.65f));
            if(view.z<=0||view.x<0||view.x>1||view.y<0||view.y>1)return;
            var worldRect=ui.WorldDisplay.rectTransform;
            var world=worldRect.TransformPoint(new Vector3(Mathf.Lerp(worldRect.rect.xMin,worldRect.rect.xMax,view.x),Mathf.Lerp(worldRect.rect.yMin,worldRect.rect.yMax,view.y),0));
            var local=(Vector2)ui.CollectionLayer.InverseTransformPoint(world);var bounds=ui.CollectionLayer.rect;
            var origin=new Vector2((local.x-bounds.xMin)/bounds.width,(local.y-bounds.yMin)/bounds.height);
            int count=Mathf.Min(gold,Mathf.Clamp(2+gold/12,3,6)),left=gold;
            for(int i=0;i<_collectionFlights.Length&&count>0;i++)
            {
                var flight=_collectionFlights[i];if(flight.Active||ui.CollectionIcons[i]==null)continue;
                int share=(left+count-1)/count;left-=share;count--;
                flight.Active=true;flight.Origin=origin;flight.Age=0;flight.Delay=count*.025f;flight.Gold=share;
                float angle=(i*2.399963f+Time.unscaledTime*.7f);flight.Scatter=new Vector2(Mathf.Cos(angle)*ui.CollectionSpread,18+Mathf.Sin(angle)*ui.CollectionSpread*.45f);
                flight.Spin=Mathf.Sin(angle)*120;_pendingHarvestGold+=share;
                var icon=ui.CollectionIcons[i];icon.sprite=kind==Kind.Rock?ui.StoneCollectionSprite:ui.WoodCollectionSprite;
                icon.rectTransform.anchoredPosition=local;icon.rectTransform.localScale=Vector3.one;icon.color=Color.white;icon.gameObject.SetActive(false);
            }
            // 풀이 가득 차면 남은 금액은 표시 목표에 바로 합류한다. 실제 지급은 모델이 이미 마쳤다.
        }
        private void UpdateGoldFeedback()
        {
            var ui=_sceneUi;float dt=Mathf.Min(Time.unscaledDeltaTime,.1f);
            if(_collectionFlights!=null&&ui.CollectionLayer!=null&&ui.GoldTarget!=null)
            {
                var bounds=ui.CollectionLayer.rect;
                Vector2 target=ui.CollectionLayer.InverseTransformPoint(ui.GoldTarget.TransformPoint(ui.GoldTarget.rect.center));
                for(int i=0;i<_collectionFlights.Length;i++)
                {
                    var flight=_collectionFlights[i];if(!flight.Active)continue;
                    var icon=ui.CollectionIcons[i];flight.Age+=dt;float age=flight.Age-flight.Delay;if(age<0)continue;
                    icon.gameObject.SetActive(true);
                    Vector2 start=new Vector2(bounds.xMin+flight.Origin.x*bounds.width,bounds.yMin+flight.Origin.y*bounds.height),scatter=start+flight.Scatter;
                    float burst=Mathf.Max(.05f,ui.CollectionBurstSeconds),duration=Mathf.Max(.05f,ui.CollectionFlightSeconds);
                    Vector2 position;float scale;
                    if(age<burst)
                    {
                        float t=age/burst;position=Vector2.Lerp(start,scatter,1-Mathf.Pow(1-t,3))+Vector2.up*(Mathf.Sin(t*Mathf.PI)*ui.CollectionArc*.4f);
                        scale=.65f+.45f*Mathf.Sin(t*Mathf.PI*.5f);
                    }
                    else
                    {
                        float t=Mathf.Clamp01((age-burst)/duration),ease=t*t;
                        Vector2 control=(scatter+target)*.5f+Vector2.up*ui.CollectionArc;
                        position=(1-ease)*(1-ease)*scatter+2*(1-ease)*ease*control+ease*ease*target;
                        scale=Mathf.Lerp(1.1f,.25f,t*t);
                        if(t>=1){flight.Active=false;_pendingHarvestGold=Mathf.Max(0,_pendingHarvestGold-flight.Gold);icon.gameObject.SetActive(false);_goldPulse=1;PlaySound(SoundCue.Gold);continue;}
                    }
                    icon.rectTransform.anchoredPosition=position;icon.rectTransform.localScale=Vector3.one*scale;
                    icon.rectTransform.localRotation=Quaternion.Euler(0,0,flight.Spin*age);
                }
                _goldPulse=Mathf.MoveTowards(_goldPulse,0,dt*5);
                ui.GoldTarget.localScale=_goldTargetScale*(1+.16f*_goldPulse);
            }
            double targetGold=Math.Max(0,_model.Money-_pendingHarvestGold);
            // 지출은 즉시 반영하고 수입만 도착 후 부드럽게 누적한다.
            if(targetGold<_displayGold)_displayGold=targetGold;
            else _displayGold+=(targetGold-_displayGold)*(1-Math.Exp(-Math.Max(1,ui.GoldCountSpeed)*dt));
            if(Math.Abs(targetGold-_displayGold)<.05)_displayGold=targetGold;
            int shown=(int)Math.Floor(_displayGold);if(shown!=_lastGoldLabel){ui.Label("HUD.Money",shown.ToString("N0"));_lastGoldLabel=shown;}
        }
        private void InitializeSceneUi()
        {
            if(_sceneUi==null){Debug.LogError("Prototype Canvas 참조가 없습니다. 씬의 Canvas와 PrototypeView.SceneUi 연결을 확인하세요.");return;}
            _font=_sceneUi.Font;_baseUiResolution=_sceneUi.Scaler.referenceResolution;
            _uiSize=PlayerPrefs.GetFloat("Afterlife.UI.Size",1);_edgeFraction=PlayerPrefs.GetFloat("Afterlife.Camera.Edge",.25f);_cameraSpeedFactor=PlayerPrefs.GetFloat("Afterlife.Camera.Speed",1);
            InitializeBuildItems();
            PrepareProgressionUi();
            _sceneUi.Bind(UiClick,UiSlider);InitializeProgressionUi();_sceneUi.WorldDisplay.texture=_pixels;
            foreach(var binding in _sceneUi.Bindings)
            {
                if(binding?.Button==null)continue;
                var feedback=binding.Button.GetComponent<PrototypeDisabledButtonSound>();
                if(feedback==null)feedback=binding.Button.gameObject.AddComponent<PrototypeDisabledButtonSound>();
                feedback.Bind(binding.Button,_audio);
            }
            var buildCatalog=_sceneUi.BuildCatalog!=null?_sceneUi.BuildCatalog:Resources.Load<PrototypeBuildCatalog>(PrototypeBuildCatalog.ResourcePath);
            if(buildCatalog!=null)_configuredBuildKinds=buildCatalog.Buildings.Where(data=>data!=null).Select(data=>data.Id).ToArray();
            InitializeRelicHud();
            _collectionFlights=new CollectionFlight[_sceneUi.CollectionIcons?.Length??0];
            for(int i=0;i<_collectionFlights.Length;i++)_collectionFlights[i]=new CollectionFlight();
            if(_sceneUi.GoldTarget!=null)_goldTargetScale=_sceneUi.GoldTarget.localScale;ResetGoldFeedback();
        }
        private void UiSlider(string id,float value)
        {
            if(id=="Menu.Size")_uiSize=value;else if(id=="Menu.Edge")_edgeFraction=value;else if(id=="Menu.Speed")_cameraSpeedFactor=value;
            SaveUiPreferences();_uiRefresh=0;
        }
        private void UiClick(string id)
        {
            if(!_ready||_gameFlow==null||!_gameFlow.IsPlaying)return;
            // 전용 결과음이 있는 작업은 클릭음을 겹치지 않는다. 보류한 연구/강화도 무음 유지.
            if(id.StartsWith("Settings.Tab.")||id.StartsWith("Research.Tab."))PlaySound(SoundCue.Open);
            else if(id.StartsWith("Recruit.")||id.StartsWith("Memory."))PlaySound(SoundCue.Select);
            if(id=="HUD.Loot")PushModal(Modal.Loot);
            else if(id=="HUD.Menu")PushModal(Modal.Menu);
            else if(id=="Menu.Settings")PushModal(Modal.Settings);
            else if(id.StartsWith("Settings.Tab."))_settingsTab=int.Parse(id.Substring("Settings.Tab.".Length));
            else if(id.EndsWith(".Close")||id=="Menu.Resume"||id=="RebirthShop.Start")CloseModal();
            else if(id=="Menu.GiveUp")
            {
                _gameFlow.ReturnToTitle();
            }
            else if(id.StartsWith("Build.Item."))
            {
                var kind=(Kind)Enum.Parse(typeof(Kind),id.Substring(11));
                if(_tileBuildMenu)
                {
                    if(_buildMenuClosing||_buildMenuTime<BuildMarkerSeconds+BuildPanelSeconds)return;
                    bool valid=_model.PlacementError(kind,(int)_buildTile.X,(int)_buildTile.Z)==null;
                    _model.Place(kind,(int)_buildTile.X,(int)_buildTile.Z);
                    if(valid&&!_model.Frozen)CloseTileBuildMenu(false,false);
                }

            }
            else if(id=="Build.Cancel")_model.CancelConstruction(_inspected);
            else if(id=="Barracks.Cancel")_model.CancelOrder(_modalBody);
            else if(id.StartsWith("Barracks.Slot."))CancelDisplayedReservation(int.Parse(id.Substring("Barracks.Slot.".Length)));
            else if(id.StartsWith("Recruit."))_model.Recruit(_modalBody,(Kind)Enum.Parse(typeof(Kind),id.Substring(8)));
            else if(id.StartsWith("Research.Tab."))_researchCategory=int.Parse(id.Substring(13));
            else if(id.StartsWith("Upgrade."))_model.BuyUpgrade((Upgrade)Enum.Parse(typeof(Upgrade),id.Substring(8)));
            else if(id=="Tower.Upgrade")_model.UpgradeTower(_modalBody);
            else if((id=="Altar.Rebirth"||id=="RebirthReview.Rebirth")&&_model.CanRebirth&&_model.BeginRebirth()){RebuildVillageView();_modal=Modal.RebirthShop;}
            else if(id.StartsWith("Memory."))_model.BuyMemory((MemoryUpgrade)Enum.Parse(typeof(MemoryUpgrade),id.Substring(7)));
            else if(id=="Settlement.Next")CloseModal();
            else if(id.StartsWith("Settlement.Choice."))_model.ChooseDailyRelic(int.Parse(id.Substring("Settlement.Choice.".Length)));
            else if(id=="Settlement.Rebirth")PushModal(Modal.Altar);
            else if(id=="Result.Restart"||id=="Menu.Restart")_gameFlow.ReturnToTitle();
            _pressActive=false;_uiRefresh=0;
        }
        private void LateUpdate()
        {
            if(!_ready||_sceneUi==null||_gameFlow==null||!_gameFlow.IsPlaying)return;
            var ui=_sceneUi;
            if(ui.WorldDisplay.texture!=_pixels)ui.WorldDisplay.texture=_pixels;
            // 1200×675의 편집 기준에 사용자 UI 배율만 적용한다.
            ui.Scaler.referenceResolution=_baseUiResolution/_uiSize;
            ui.Show("ModalLayer",_modal!=Modal.None||_model.Ended);
            foreach(Modal m in Enum.GetValues(typeof(Modal)))if(m!=Modal.None)
            {
                bool top=_modal==m&&!_model.Ended;
                bool visible=top||_modalStack.Exists(entry=>entry.modal==m);
                ui.Show("Modal."+m,visible);
                var rect=ui.Item("Modal."+m)?.Rect;
                if(rect==null)continue;
                var group=rect.GetComponent<CanvasGroup>();
                if(group==null)group=rect.gameObject.AddComponent<CanvasGroup>();
                group.interactable=top;group.blocksRaycasts=top;
            }
            var topRect=ui.Item(_model.Ended?"Modal.Result":"Modal."+_modal)?.Rect;
            if(topRect!=null)topRect.SetAsLastSibling();
            if(ui.ModalBackdrop!=null)
            {
                int darkLayers=0;bool blur=false;
                AccumulateBackdrop(topRect,ref darkLayers,ref blur);
                if(!_model.Ended)
                    foreach(var entry in _modalStack)
                        AccumulateBackdrop(ui.Item("Modal."+entry.modal)?.Rect,ref darkLayers,ref blur);
                // 효과를 모두 꺼도 투명한 배경으로 뒤쪽 UI 클릭을 차단한다.
                ui.ModalBackdrop.SetVisible(_modal!=Modal.None||_model.Ended,topRect,darkLayers,blur);
            }
            UpdateTileBuildMenu();ui.Show("Modal.Result",_model.Ended);ui.Show("HUD.Build",false);ui.Show("Build",_tileBuildMenu&&_modal==Modal.None&&!_model.Ended);
            if(_shownModal!=_modal)
            {
                if(_modalStack.Count>=_shownModalDepth&&_modal!=Modal.None&&_modal!=Modal.Settlement&&_modal!=Modal.RebirthShop)PlaySound(SoundCue.Open);
                _shownModal=_modal;
                var panel=ui.Item("Modal."+_modal);if(panel!=null)foreach(var scroll in panel.Rect.GetComponentsInChildren<ScrollRect>(true))scroll.verticalNormalizedPosition=1;
            }
            _shownModalDepth=_modalStack.Count;
            UpdateUiTimeline();UpdateUiFeedback();UpdateGoldFeedback();UpdateDayBanner();
            if(Time.unscaledTime<_uiRefresh)return;_uiRefresh=Time.unscaledTime+.10f;
            ui.Label("HUD.Population",$"주민 {_model.Population} / {_model.Capacity}     병력 {_model.Bodies.Count(b=>b.Alive&&b.Friendly)}");
            foreach(var id in new[]{"HUD.Loot","HUD.Menu"})ui.Enable(id,_modal==Modal.None&&!_model.Ended);
            ui.Enable("Menu.GiveUp",true);

            SyncRelicHud();
            foreach(var k in _configuredBuildKinds)
            {
                string requirement=_model.ConstructionRequirement(k);
                ui.Label("Build.Cost."+k,requirement??$"골드 {_model.ConstructionPrice(k)}");ui.Enable("Build.Item."+k,requirement==null);
            }
            var inspected=_model.Find(_inspected);ui.Enable("Build.Cancel",inspected!=null&&inspected.Building&&!inspected.Complete);
            ui.Label("HUD.Notice",Time.unscaledTime-_noticeTime<4.5f?_notice:"");ui.Show("HUD.Notice",Time.unscaledTime-_noticeTime<4.5f&&_notice.Length>0);
            ui.Show("HUD.BuildHint",_model.Mode==InputMode.Build&&_modal==Modal.None);ui.Label("HUD.BuildHint",$"{VillageModel.Name(_model.BuildKind)} 배치 · ESC 취소");
            ui.Show("HUD.Performance",_showPerformance);ui.Label("HUD.Performance",$"FPS {_averageFps:0} · 최대 {_lastFrameMax:0.0}ms\n모델 {_modelMilliseconds:0.00}ms · 표시 {_syncMilliseconds:0.00}ms");
            if(_model.Ended){ui.Label("Result.Title",_model.Phase==Phase.Victory?"마을에 찾아온 평화":"마지막 불빛이 꺼졌습니다");ui.Label("Result.Body",$"최고 {_model.LastResult?.BestCleared}일 방어 · 환생 {_model.LastResult?.Rebirths}회\n처치 {_model.LastResult?.Kills} · 새로운 판은 처음부터 시작합니다.\n\n"+(_model.BannerUnlocked?"해금: 청록 마을 깃발":"2일 이상 방어하면 청록 마을 깃발이 해금됩니다."));}
            UpdateUiModal();
        }
        private void AccumulateBackdrop(RectTransform rect,ref int darkLayers,ref bool blur)
        {
            if(rect==null)return;
            var surface=rect.GetComponent<PrototypeModalSurface>();
            if(surface==null||surface.DarkenBackground)darkLayers++;
            // 컴포넌트가 없는 기존 모달은 종전의 배경 설정을 사용한다.
            blur|=surface!=null?surface.BlurBackground:_sceneUi.ModalBackdrop.Mode==ModalBackdropMode.DarkBlur;
        }
        private void UpdateUiModal()
        {
            var ui=_sceneUi;var b=_model.Find(_modalBody);
            if(_modal==Modal.Loot)
            {
                ui.Label("Loot.Total",$"예상 정산액 · 골드 {_model.PendingLootMoney+_model.Valuables*5:N0}");
                foreach(LootKind k in Enum.GetValues(typeof(LootKind))){_model.Loot.TryGetValue(k,out int count);ui.Show("Loot.Row."+k,count>0);ui.Label("Loot.Row."+k,$"{VillageModel.LootName(k)} ×{count:N0}     골드 {count*VillageModel.LootValue(k):N0}");}
                ui.Show("Loot.Valuables",_model.Valuables>0);ui.Label("Loot.Valuables",$"귀중품 ×{_model.Valuables}     골드 {_model.Valuables*5}");ui.Show("Loot.Empty",_model.PendingLootMoney==0&&_model.Valuables==0);
            }
            if(_modal==Modal.Settings)
            {
                SetUiSlider("Menu.Size",_uiSize);SetUiSlider("Menu.Edge",_edgeFraction);SetUiSlider("Menu.Speed",_cameraSpeedFactor);
                ui.Label("Menu.Size.Label",$"UI 크기 {_uiSize*100:0}%");ui.Label("Menu.Edge.Label",$"카메라 이동 구간 {_edgeFraction*100:0}%");ui.Label("Menu.Speed.Label",$"카메라 속도 {_cameraSpeedFactor:0.0}배");
                for(int tab=0;tab<2;tab++)
                {
                    bool selected=tab==_settingsTab;
                    ui.Show("Settings.Page."+tab,selected);
                    var binding=ui.Item("Settings.Tab."+tab);
                    if(binding?.Button==null)continue;
                    // 활성 탭은 사용 불가 상태가 아니라 선택된 페이지를 나타낸다.
                    binding.Button.interactable=true;
                    if(binding.Button.targetGraphic!=null)
                        binding.Button.targetGraphic.color=selected?new Color(.12f,.26f,.30f,1):new Color(.09f,.14f,.20f,1);
                    if(binding.TmpText!=null)
                        binding.TmpText.color=selected?new Color(.48f,.9f,.86f,1):new Color(.96f,.95f,.88f,1);
                }
            }
            if(_modal==Modal.Barracks&&b!=null)
            {
                UpdateBarracksUi(b);
            }
            if(_modal==Modal.Tower&&b!=null){ui.Label("Tower.Info",$"{VillageModel.Name(b.Kind)} {b.TowerLevel}/5\n피해 {_model.TowerDamage(b):0.#} · 사거리 {_model.TowerRange(b):0.##}\n동시 공격 {_model.TargetCount} · 체력 {b.Health:0}/{b.MaxHealth:0}");ui.Label("Tower.Upgrade",b.TowerLevel>=5?"최대 단계":$"타워 강화 · 골드 {_model.TowerPrice(b)}");ui.Enable("Tower.Upgrade",b.Complete&&!b.MovingOut&&b.TowerLevel<5&&_model.Money>=_model.TowerPrice(b));}
            if(_modal==Modal.RebirthReview){var r=_model.PreviewRebirth();ui.Label("RebirthReview.Info",$"이번 생의 기록\n{r.Cleared}일 방어 · {r.Kills}마리 처치\n진행 {r.WaveReward} + 처치 {r.KillBonus} + 재화 {r.StockBonus}\n\n에테론 +{r.Total} · 보유 {_model.Etheron}\n집 하나와 첫날부터 다시 시작합니다.");ui.Enable("RebirthReview.Rebirth",_model.CanRebirth);ui.Show("RebirthReview.Lock",!_model.CanRebirth);}
            if(_modal==Modal.RebirthShop){ui.Label("RebirthShop.Total",$"보유 에테론 {_model.Etheron} · 환생 {_model.RebirthCount}회");foreach(MemoryUpgrade u in Enum.GetValues(typeof(MemoryUpgrade))){int level=_model.MemoryLevel(u),price=_model.MemoryPrice(u);ui.Label("Memory."+u,level>=3?"최대 단계":$"기억 강화 {level}/3 · 에테론 {price}");ui.Enable("Memory."+u,level<3&&_model.Etheron>=price);}}
            if(_modal==Modal.Settlement)
            {
                bool choosing=_model.DailyRewardChoosing;
                ui.Show("Settlement.Rewards",choosing);ui.Show("Settlement.Info",!choosing);
                ui.Show("Settlement.Help",!choosing);ui.Show("Settlement.Next",!choosing);ui.Show("Settlement.Rebirth",!choosing);
                ui.Enable("Settlement.Close",!choosing);
                ui.Label("Settlement.Info",$"{_model.Wave}일차 결산\n귀중품 {_model.LastValuables}개\n{_model.LastLootSummary}\n\n받은 골드 {_model.LastSettlement} · 보유 골드 {_model.Money:0}"+(_model.DailyRewardPending?"\n생존 보상: 유물 3종 중 1개 선택":"\n생존 보상을 받았습니다."));
                ui.Label("Settlement.Next",_model.DailyRewardPending?"생존 보상 선택":_model.Wave>=_model.Settings.Days?"결과 확인":"다음 낮 시작");ui.Enable("Settlement.Rebirth",_model.CanRebirth);
                for(int i=0;i<3;i++)
                {
                    bool available=i<_model.DailyRelicChoices.Count;ui.Show("Settlement.Choice."+i,choosing&&available);
                    if(!available)continue;var kind=_model.DailyRelicChoices[i];int level=_model.RelicLevel(kind);
                    var data=_relicCatalog!=null?_relicCatalog.Find(kind):null;
                    ui.Label("Settlement.Choice."+i,(level==0?"새 유물":$"{level} → {level+1}단계")+"\n"+(data!=null?data.Tooltip(level+1):VillageModel.RelicName(kind)+"\n"+VillageModel.RelicEffect(kind,level+1)));
                }
            }
        }
        private void SetUiSlider(string id,float value){var slider=_sceneUi.Item(id)?.Slider;if(slider!=null)slider.SetValueWithoutNotify(value);}
        private void UpdateUiTimeline()
        {
            var ui=_sceneUi;bool night=_model.Phase==Phase.Night||_model.Phase==Phase.Dusk||_model.Phase==Phase.Settlement;
            if(ui.DayNightIcon!=null){var sprite=night?ui.NightMoonSprite:ui.DaySunSprite;if(ui.DayNightIcon.sprite!=sprite)ui.DayNightIcon.sprite=sprite;}
            ui.Show("HUD.Sun",false);ui.Show("HUD.Moon",false);ui.Show("HUD.Day",false);
            ui.Label("DayNight.Text",$"{_model.Wave}일차 · "+(_model.Phase==Phase.Day?"낮":_model.Phase==Phase.Dusk?"황혼":_model.Phase==Phase.Night?"밤":"정산")+(_model.Phase==Phase.Night?$" · 남은 적 {_model.RemainingRaiders}":$" · {_model.Remaining:0}초"));
            float total=_settings.DaySeconds+_settings.DuskSeconds+_settings.NightSeconds,elapsed=_model.Phase==Phase.Day?_settings.DaySeconds-_model.Remaining:_model.Phase==Phase.Dusk?_settings.DaySeconds+_settings.DuskSeconds-_model.Remaining:_model.Phase==Phase.Night?total-_model.Remaining:total;
            float width=ui.Item("HUD.Track").Rect.rect.width,content=Mathf.Max(width,_model.Settings.Days*90),progress=(_model.Wave-1+Mathf.Clamp01(elapsed/total))/_model.Settings.Days*content,scroll=Mathf.Clamp(progress-width/2,0,content-width),visible=Mathf.Clamp(progress-scroll,0,width);
            var marker=ui.Item("HUD.Marker").Rect;var pos=marker.anchoredPosition;pos.x=Mathf.Clamp(visible,8,width-8);marker.anchoredPosition=pos;
            var progressRect=ui.Item("HUD.Progress").Rect;progressRect.sizeDelta=new Vector2(visible,progressRect.sizeDelta.y);
            for(int i=0;i<32;i++){float x=i*content/_model.Settings.Days-scroll;ui.Show("DayTick."+i,i<=_model.Settings.Days&&x>=0&&x<=width);var tick=ui.Item("DayTick."+i);if(tick==null)continue;pos=tick.Rect.anchoredPosition;pos.x=x;tick.Rect.anchoredPosition=pos;ui.Label("DayTick."+i,i==_model.Settings.Days?"끝":(i+1).ToString());}
        }
        private void UpdateUiFeedback()
        {
            var ui=_sceneUi;Vector2 mouse=Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero;string tip="";
            if(_modal==Modal.None)
            {
                tip=RelicTooltip(mouse);
                if(ui.Contains("HUD.Loot",mouse))tip="전리품\n획득한 전리품과 귀중품을 확인합니다. 하루 결산 시 골드로 정산됩니다.";
                if(ui.Contains("HUD.GoldIcon",mouse)||ui.Contains("HUD.Money",mouse))tip=$"보유 골드 {Mathf.FloorToInt(_model.Money):N0}\n채집한 자원은 즉시 골드가 됩니다.";
                if(ui.Contains("HUD.Menu",mouse))tip="메뉴 · ESC · 메뉴를 열면 일시정지됩니다.";
                var body=_model.Find(_hovered);if(tip.Length==0&&body!=null&&!ui.Blocks(mouse))
                {
                    tip=ResourceName(body);
                    if(body.Kind==Kind.Tree||body.Kind==Kind.Rock)tip+=$"\n남은 자원 {body.Health:0.#} / {body.MaxHealth:0.#}\n채집 1당 골드 {(body.Kind==Kind.Tree?1:2)*_model.HarvestGoldMultiplier:0.##}";
                    else if(body.Kind==Kind.Ruins||body.Kind==Kind.AbandonedAltar)
                    {
                        bool active=body.Kind==Kind.Ruins?_model.ActiveRuins:_model.ActiveShrine;
                        tip+="\n마을 영역 편입 시: "+(body.Kind==Kind.Ruins?"타워 사거리 +1":"자동 건설 속도 +25%");
                        tip+="\n"+(active?"같은 종류 편입 효과 적용 중 (중첩 없음)":"현재 미적용 · 집으로 마을 영역을 넓히세요.");
                    }
                    else tip+=$"\n체력 {body.Health:0}/{body.MaxHealth:0}";
                    if(body.SpiritShield>0)tip+=$"\n영혼 보호막 {body.SpiritShield:0}";
                    if(body.Building)tip+=$"\n건설 {body.Construction*100:0}% · 클릭 관리 / 드래그 이전";
                    if(VillageModel.IsTower(body.Kind))tip+=$"\n피해 {_model.TowerDamage(body):0.#} · 범위 {_model.TowerRange(body):0.#}";
                }
            }
            ui.Tooltip.gameObject.SetActive(tip.Length>0);ui.SetTooltipText(tip);
            if(tip.Length>0)
            {
                var canvas=(RectTransform)ui.transform;
                var camera=ui.Canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:ui.Canvas.worldCamera;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,mouse,camera,out var p);
                // 프리팹의 앵커/피벗을 바꾸지 않고 실제 경계의 왼쪽 위를 커서에 맞춘다.
                var bounds=RectTransformUtility.CalculateRelativeRectTransformBounds(canvas,ui.Tooltip);
                float x=Mathf.Clamp(p.x+20,canvas.rect.xMin,Mathf.Max(canvas.rect.xMin,canvas.rect.xMax-bounds.size.x));
                float y=Mathf.Clamp(p.y-20,Mathf.Min(canvas.rect.yMax,canvas.rect.yMin+bounds.size.y),canvas.rect.yMax);
                ui.Tooltip.position+=canvas.TransformVector(new Vector3(x-bounds.min.x,y-bounds.max.y,0));
            }
            bool selection=_pressActive&&_dragging&&!_commandDrag&&!_buildingDrag;ui.Selection.gameObject.SetActive(selection);
            if(selection){var canvas=(RectTransform)ui.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,_pressScreen,null,out var a);RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas,mouse,null,out var b);ui.Selection.anchoredPosition=new Vector2(Mathf.Min(a.x,b.x),Mathf.Max(a.y,b.y));ui.Selection.sizeDelta=new Vector2(Mathf.Abs(a.x-b.x),Mathf.Abs(a.y-b.y));}
            int index=0;foreach(var amount in _amounts)
            {
                float t=(_effectTime-amount.Born)/1.05f;if(t<0||t>=1||_model.Visibility(new Point(amount.Position.x,amount.Position.z))<=0)continue;var p=_camera.WorldToViewportPoint(amount.Position);if(p.z<=0||index>=ui.Amounts.Length)continue;
                var text=ui.Amounts[index++];text.gameObject.SetActive(true);text.text=amount.Text;text.fontSize=amount.Kind==FeedbackKind.Critical?ui.CriticalContributionSize:ui.NormalContributionSize;
                Color color=amount.Kind==FeedbackKind.Lightning?new Color(.65f,.8f,1):amount.Kind==FeedbackKind.EnemyDamage?ui.EnemyDamageColor:amount.Kind==FeedbackKind.Critical?ui.CriticalContributionColor:amount.Kind==FeedbackKind.Fire?ui.FireContributionColor:amount.Kind==FeedbackKind.Burn?ui.BurnContributionColor:ui.NormalContributionColor;color.a=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.6f,1,t));text.color=color;
                var rt=text.rectTransform;rt.anchorMin=rt.anchorMax=new Vector2(p.x,p.y);rt.anchoredPosition=new Vector2(amount.Offset/ui.Canvas.scaleFactor,48*(1-Mathf.Pow(1-t,2))/ui.Canvas.scaleFactor);
            }
            for(;index<ui.Amounts.Length;index++)ui.Amounts[index].gameObject.SetActive(false);
        }
    }
}
