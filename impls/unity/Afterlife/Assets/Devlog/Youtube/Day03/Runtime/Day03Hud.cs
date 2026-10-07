using TMPro;
using Afterlife.Modules.HarvestFeedback;
using UnityEngine;
using UnityEngine.UI;
namespace Afterlife.Devlog.Day03
{
    public sealed class Day03Hud
    {
        private readonly GameObject _root;
        private readonly GameObject _goldHud;
        private readonly TextMeshProUGUI _gold, _farm, _house, _guide, _comparison;
        private readonly GameObject _comparisonPanel;
        private readonly Image _farmBar, _houseBar, _productionBar;
        private float _rewardTime;
        private readonly PointerInputPresenter _pointer;
        private readonly HarvestFeedbackPresenter _harvestFeedback;
        private readonly Sprite _barleySprite;
        private readonly Sprite _woodSprite;
        private readonly GameObject _buildMenu;
        private readonly UnityEngine.UI.Button _farmButton,_houseButton;
        private readonly TextMeshProUGUI _buildHint;
        private readonly GameObject _buildStatus;
        private readonly TextMeshProUGUI _buildModeTitle;
        private float _selectionTime,_placementTime;
        private VillageTarget _placementTarget;
        private int _placementCost;
        private bool _showHouseCompletion;
        public event System.Action<VillageTarget> BuildSelected;
        public Day03Hud(Transform parent, TMP_FontAsset font, Sprite goldSprite, Sprite barleySprite, Sprite woodSprite)
        {
            _root=new GameObject("Day03Hud",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
            _root.transform.SetParent(parent,false);
            _root.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
            _root.GetComponent<Canvas>().sortingOrder=100;
            var scaler=_root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            _goldHud=new GameObject("GoldHud",typeof(RectTransform));
            _goldHud.transform.SetParent(_root.transform,false);
            var goldRect=_goldHud.GetComponent<RectTransform>();
            goldRect.anchorMin=new Vector2(.06f,.80f);goldRect.anchorMax=new Vector2(.30f,.87f);
            goldRect.offsetMin=goldRect.offsetMax=Vector2.zero;
            var iconObject=new GameObject("GoldIcon",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            iconObject.transform.SetParent(_goldHud.transform,false);
            var icon=iconObject.GetComponent<UnityEngine.UI.Image>();
            icon.sprite=goldSprite;icon.preserveAspect=true;icon.raycastTarget=false;icon.enabled=goldSprite!=null;
            icon.rectTransform.anchorMin=icon.rectTransform.anchorMax=new Vector2(0,.5f);
            icon.rectTransform.anchoredPosition=new Vector2(40,0);icon.rectTransform.sizeDelta=new Vector2(48,48);
            _gold=Label("GoldAmount",font,0,0,1,1,38);
            _gold.transform.SetParent(_goldHud.transform,false);
            _gold.rectTransform.offsetMin=new Vector2(78,0);
            _gold.color=new Color(1,.83f,.35f);
            _farm=Label("Farm",font,.06f,.71f,.34f,.77f,24);
            _house=Label("House",font,.06f,.63f,.34f,.69f,24);
            _comparisonPanel=new GameObject("VillageSummary",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            _comparisonPanel.transform.SetParent(_root.transform,false);
            var summaryRect=_comparisonPanel.GetComponent<RectTransform>();summaryRect.anchorMin=new Vector2(.33f,.25f);summaryRect.anchorMax=new Vector2(.89f,.36f);summaryRect.offsetMin=summaryRect.offsetMax=Vector2.zero;
            var summaryImage=_comparisonPanel.GetComponent<UnityEngine.UI.Image>();summaryImage.color=new Color(.04f,.07f,.09f,.95f);summaryImage.raycastTarget=false;
            _comparison=Label("VillageSummaryText",font,.03f,.08f,.97f,.92f,27);
            _comparison.transform.SetParent(_comparisonPanel.transform,false);_comparison.alignment=TextAlignmentOptions.Center;
            _comparisonPanel.SetActive(false);
            _guide=Label("Guide",font,.06f,.20f,.94f,.30f,20);
            _farmBar=Bar("ConstructionProgress",.06f,.70f,.30f,.71f);
            _houseBar=Bar("HouseProgress",.06f,.62f,.30f,.63f);
            _productionBar=Bar("ProductionProgress",.06f,.70f,.30f,.71f);
            _pointer=_root.AddComponent<PointerInputPresenter>();_pointer.Initialize(font);
            _buildMenu=new GameObject("BuildMenu",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            _buildMenu.transform.SetParent(_root.transform,false);
            var panel=_buildMenu.GetComponent<RectTransform>();panel.anchorMin=new Vector2(.05f,.32f);panel.anchorMax=new Vector2(.29f,.60f);panel.offsetMin=panel.offsetMax=Vector2.zero;
            _buildMenu.GetComponent<UnityEngine.UI.Image>().color=new Color(.04f,.07f,.09f,.95f);
            _buildModeTitle=Label("BuildModeTitle",font,.06f,.81f,.94f,.98f,30);
            _buildModeTitle.transform.SetParent(_buildMenu.transform,false);
            _buildModeTitle.fontStyle=FontStyles.Bold;_buildModeTitle.color=new Color(1,.83f,.35f);
            _buildModeTitle.text="건설 모드";
            _farmButton=BuildButton("SelectFarm",font,"농장",.43f,.74f,VillageTarget.Farm);
            _houseButton=BuildButton("SelectHouse",font,"집",.06f,.37f,VillageTarget.SecondHouse);
            _buildStatus=new GameObject("BuildStatus",typeof(RectTransform),typeof(UnityEngine.UI.Image));
            _buildStatus.transform.SetParent(_root.transform,false);
            var statusRect=_buildStatus.GetComponent<RectTransform>();statusRect.anchorMin=new Vector2(.33f,.25f);statusRect.anchorMax=new Vector2(.89f,.36f);statusRect.offsetMin=statusRect.offsetMax=Vector2.zero;
            var statusImage=_buildStatus.GetComponent<UnityEngine.UI.Image>();statusImage.color=new Color(.04f,.07f,.09f,.95f);statusImage.raycastTarget=false;
            _buildHint=Label("BuildHint",font,.03f,.08f,.97f,.92f,27);
            _buildHint.transform.SetParent(_buildStatus.transform,false);
            _buildHint.alignment=TextAlignmentOptions.Center;
            _buildMenu.SetActive(false);_buildStatus.SetActive(false);
            _barleySprite=barleySprite;
            _woodSprite=woodSprite;
            _harvestFeedback=_root.AddComponent<HarvestFeedbackPresenter>();
            _harvestFeedback.Initialize(font,icon.rectTransform,null,null);
            _harvestFeedback.IconSize=48;_harvestFeedback.Spread=96;_harvestFeedback.Arc=120;
            _harvestFeedback.IconArrived+=Reward;
        }
        private UnityEngine.UI.Button BuildButton(string name,TMP_FontAsset font,string title,float bottom,float top,VillageTarget kind)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image),typeof(UnityEngine.UI.Button));
            go.transform.SetParent(_buildMenu.transform,false);
            var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.04f,bottom);rect.anchorMax=new Vector2(.96f,top);rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<UnityEngine.UI.Image>();image.color=new Color(.19f,.25f,.28f);
            var button=go.GetComponent<UnityEngine.UI.Button>();button.targetGraphic=image;button.onClick.AddListener(()=>BuildSelected?.Invoke(kind));
            // Automatic playback disables input, but its selected state must stay fully legible.
            button.transition=UnityEngine.UI.Selectable.Transition.None;
            var outline=go.AddComponent<UnityEngine.UI.Outline>();outline.effectDistance=new Vector2(2,-2);outline.effectColor=new Color(.35f,.45f,.48f);
            var label=new GameObject("Title",typeof(RectTransform),typeof(TextMeshProUGUI));label.transform.SetParent(go.transform,false);
            var text=label.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=25;text.alignment=TextAlignmentOptions.Center;text.text=title;text.raycastTarget=false;
            var r=text.rectTransform;r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
            return button;
        }
        private TextMeshProUGUI Label(string name,TMP_FontAsset font,float x,float y,float right,float top,float size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(_root.transform,false);
            var text=go.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=size;text.color=Color.white;
            text.alignment=TextAlignmentOptions.MidlineLeft;text.raycastTarget=false;
            var rect=text.rectTransform;rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(right,top);rect.offsetMin=rect.offsetMax=Vector2.zero;
            return text;
        }
        private Image Bar(string name,float x,float y,float right,float top)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(_root.transform,false);
            var image=go.GetComponent<Image>();image.color=new Color(.95f,.76f,.28f);image.raycastTarget=false;
            var rect=image.rectTransform;rect.anchorMin=new Vector2(x,y);rect.anchorMax=new Vector2(right,top);rect.offsetMin=rect.offsetMax=Vector2.zero;
            return image;
        }
        public void Reward() => _rewardTime=1.4f;
        public void ClearTransient(){_rewardTime=0;_selectionTime=0;_placementTime=0;_showHouseCompletion=false;_farmButton.transform.localScale=_houseButton.transform.localScale=Vector3.one;_buildStatus.SetActive(false);_comparisonPanel.SetActive(false);_pointer.Clear();_harvestFeedback.Clear();}
        public void ShowSelection()=>_selectionTime=.32f;
        public void ShowPlacement(VillageTarget target,int cost){_placementTarget=target;_placementCost=cost;_placementTime=1.6f;_showHouseCompletion=false;}
        public void ShowHouseCompletion(){_showHouseCompletion=true;_placementTime=2;}
        public void ShowHarvest(Vector2 screenPoint)
        {
            _harvestFeedback.ShowText(screenPoint,"목재 +1",new Color(1,.87f,.53f));
            if(_woodSprite!=null)_harvestFeedback.ShowCollection(screenPoint,_woodSprite,1);
            else Reward();
        }
        public void ShowProduction(Vector2 screenPoint,int amount)
        {
            // Economy events precede this frame's HUD visibility update.
            if(amount<=0)return;
            _harvestFeedback.ShowText(screenPoint,"보리 +"+amount,new Color(1,.83f,.35f));
            if(_barleySprite!=null)_harvestFeedback.ShowCollection(screenPoint,_barleySprite,amount);
            else Reward();
        }
        public void PresentInput(float delta,bool visible,Vector2 point,bool clickMode,bool pressed,bool held)=>_pointer.Tick(delta,visible,point,clickMode,pressed,held);
        public bool BlocksWorldInput(Vector2 point)=>_buildMenu.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(_buildMenu.GetComponent<RectTransform>(),point);
        public Vector2 BuildButtonScreenPoint(VillageTarget kind)
        {
            Canvas.ForceUpdateCanvases();
            return RectTransformUtility.WorldToScreenPoint(null,(kind==VillageTarget.Farm?_farmButton:_houseButton).transform.position);
        }
        public void PresentBuild(VillageSession session,Day03Settings settings,bool mode,bool inputBlocked,VillageTarget selected,string error,bool hasTile=false,bool frozen=false)
        {
            _buildMenu.SetActive(mode);_buildStatus.SetActive(mode||_placementTime>0);
            _farmButton.interactable=!inputBlocked&&!session.FarmPurchased;_houseButton.interactable=!inputBlocked&&!session.SecondHousePurchased;
            if(frozen)return;
            PresentBuilding(_farmButton,"농장",settings.FarmCost,selected==VillageTarget.Farm,session.FarmPurchased,session.FarmComplete);
            PresentBuilding(_houseButton,"집",settings.HouseCost,selected==VillageTarget.SecondHouse,session.SecondHousePurchased,session.SecondHouseComplete);
            bool purchased=selected==VillageTarget.Farm?session.FarmPurchased:selected==VillageTarget.SecondHouse&&session.SecondHousePurchased;
            var prompt=BuildPrompt.Resolve(selected,hasTile,error,purchased,session.SecondHouseComplete);
            _buildHint.color=prompt.Blocked?new Color(1,.65f,.55f):Color.white;
            _buildHint.text="<b><size=32>"+prompt.Title+"</size></b>\n"+prompt.Action;
            if(_placementTime>0){
                _buildHint.color=new Color(.65f,1,.78f);
                _buildHint.text=_showHouseCompletion
                    ?"<b><size=32>새 집 건설 완료</size></b>\n마을 영역이 넓어졌습니다"
                    :"<b><size=32>"+(_placementTarget==VillageTarget.Farm?"농장을":"새 집을")+" 짓기 시작했습니다</size></b>\n건설비 "+_placementCost+"골드 사용";
            }
        }
        private void PresentBuilding(UnityEngine.UI.Button button,string name,int cost,bool selected,bool purchased,bool complete)
        {
            var label=button.GetComponentInChildren<TextMeshProUGUI>();
            label.text=purchased?name+(complete?" · 건설 완료":" · 공사 중"):selected?"<b>"+name+" 선택됨</b>\n"+cost+"골드":name+" · "+cost+"골드";
            label.color=purchased?new Color(.65f,.7f,.72f):Color.white;
            button.GetComponent<UnityEngine.UI.Image>().color=purchased?new Color(.12f,.16f,.18f):selected?new Color(.14f,.48f,.32f):new Color(.19f,.25f,.28f);
            button.GetComponent<UnityEngine.UI.Outline>().effectColor=selected&&!purchased?new Color(1,.83f,.35f):new Color(.35f,.45f,.48f);
            float pulse=selected&&!purchased&&_selectionTime>0?.07f*Mathf.Sin(Mathf.PI*(1-_selectionTime/.32f)):0;
            button.transform.localScale=Vector3.one*(1+pulse);
        }
        public void Tick(VillageSession session,Day03Settings settings,float delta,bool guide,bool hidden,string comparison,bool preview,bool comparisonOnly=false)
        {
            _rewardTime=Mathf.Max(0,_rewardTime-delta);_root.SetActive(!hidden);
            _selectionTime=Mathf.Max(0,_selectionTime-delta);_placementTime=Mathf.Max(0,_placementTime-delta);
            if(hidden||comparisonOnly)_harvestFeedback.Clear();
            else _harvestFeedback.Tick(delta);
            _goldHud.SetActive(!comparisonOnly);_farm.gameObject.SetActive(!comparisonOnly);_house.gameObject.SetActive(!comparisonOnly&&!session.SecondHouseComplete);
            // Each arriving resource icon represents one gold, including initial wood harvesting.
            _gold.text=session.Gold.ToString("N0")+(_rewardTime>0?"  <size=24><color=#8FE6AF>+1</color></size>":"");
            _farm.text=!session.FarmPurchased?"농장 · "+settings.FarmCost+"골드":session.FarmComplete?"농장 · "+settings.ProductionSeconds+"초마다 "+settings.ProductionGold+"골드":"농장 · 공사 "+Mathf.RoundToInt(session.FarmProgress*100)+"%";
            _house.text=!session.SecondHousePurchased?"다음 집 · 건설비 "+settings.HouseCost+"골드":"새 집 공사 · "+Mathf.RoundToInt(session.HouseProgress*100)+"%";
            _farmBar.rectTransform.anchorMax=new Vector2(.06f+.24f*session.FarmProgress,.71f);
            _houseBar.rectTransform.anchorMax=new Vector2(.06f+.24f*session.HouseProgress,.63f);
            _productionBar.gameObject.SetActive(!comparisonOnly&&session.FarmComplete);
            _farmBar.gameObject.SetActive(!comparisonOnly&&session.FarmPurchased&&!session.FarmComplete);
            _houseBar.gameObject.SetActive(!comparisonOnly&&session.SecondHousePurchased&&!session.SecondHouseComplete);
            _productionBar.rectTransform.anchorMax=new Vector2(.06f+.24f*session.ProductionProgress,.71f);
            _comparison.text=comparison;_comparisonPanel.SetActive(!string.IsNullOrEmpty(comparison));
            _guide.gameObject.SetActive(guide&&!comparisonOnly&&!_buildMenu.activeSelf&&_placementTime<=0);
            _guide.text=(preview?"미리보기 · ":"")+"Ctrl/Shift 건설 · 클릭 배치 · 버튼 유지 채집/공사\nF8 직접 · F9 자동 · ←/→ 샷 이동 · Space 정지 · R 초기화 · H 안내";
        }
        public void Dispose(){_harvestFeedback.IconArrived-=Reward;_harvestFeedback.Clear();if(_root!=null)Object.Destroy(_root);}
    }
}
