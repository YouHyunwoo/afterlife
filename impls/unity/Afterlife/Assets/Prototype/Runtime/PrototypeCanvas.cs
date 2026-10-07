using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
namespace Afterlife.Prototype
{
    [Serializable] public sealed class PrototypeUiBinding
    {
        public string Id;public RectTransform Rect;public Text Text;public TMP_Text TmpText;public Button Button;public Slider Slider;public Image Image;
    }
    // 씬 UI 참조와 동적 유물 목록의 데이터/부모 참조.
    public sealed class PrototypeCanvas : MonoBehaviour
    {
        public int LayoutVersion;public int PointerControlsVersion;public TMP_FontAsset ResourcesTmpFont;public Canvas Canvas;public CanvasScaler Scaler;public RawImage WorldDisplay;public Font Font;
        public List<PrototypeUiBinding> Bindings=new List<PrototypeUiBinding>();
        public List<RectTransform> CameraGuards=new List<RectTransform>();
        [Header("유물 목록")]
        public PrototypeRelicCatalog RelicCatalog;
        public RectTransform RelicRoot;
        [Header("건설 목록 · 게임 초기화 시 생성")]
        public PrototypeBuildCatalog BuildCatalog;
        [Header("모달")]
        public int ModalSettingsVersion;
        public PrototypeModalBackdrop ModalBackdrop;
        [Tooltip("타이틀 씬을 만든 뒤 Build Settings에 등록한 씬 이름을 지정합니다.")]
        public string TitleSceneName="";
        [Header("기여 수치 스타일")]
        public int NormalContributionSize=15,CriticalContributionSize=23;
        public Color NormalContributionColor=Color.white,CriticalContributionColor=new Color(1,.76f,.12f),FireContributionColor=new Color(1,.38f,.08f),BurnContributionColor=new Color(1,.58f,.28f);
        public Color EnemyDamageColor=new Color(1,.27f,.25f);
        public RectTransform Tooltip,Selection;public Text TooltipText;public Text[] Amounts;
        public TMP_Text TooltipTmpText;
        [Header("DayNight 표시")]
        public Image DayNightIcon;
        public Sprite DaySunSprite,NightMoonSprite;
        [Header("일차 시작 배너")]
        public RectTransform DayBanner;
        public Text DayBannerText;
        [HideInInspector] public float DayBannerSeconds=1; // 이전 씬 호환. 새 시간 설정은 PrototypeDayStartBanner에서 관리.
        [Header("채집 골드 연출")]
        public RectTransform CollectionLayer,GoldTarget;
        public Image[] CollectionIcons;
        public Sprite WoodCollectionSprite,StoneCollectionSprite;
        [Min(.05f)] public float CollectionBurstSeconds=.25f,CollectionFlightSeconds=.55f;
        [Min(1)] public float CollectionSpread=48,CollectionArc=70,GoldCountSpeed=12;
        private readonly Dictionary<string,PrototypeUiBinding> _map=new Dictionary<string,PrototypeUiBinding>();
        private readonly List<RaycastResult> _hits=new List<RaycastResult>();
        private PointerEventData _pointer;
        public void Bind(Action<string> click,Action<string,float> change)
        {
            BindNoticeMessage();
            BindTooltipText();
            var menu=Bindings.Find(b=>b!=null&&b.Id=="Modal.Menu");
            if(menu!=null&&menu.Rect!=null)menu.Rect.GetComponent<PrototypeMenuPanel>()?.Connect(this);
            var loot=Bindings.Find(b=>b!=null&&b.Id=="Modal.Loot");
            if(loot!=null&&loot.Rect!=null)loot.Rect.GetComponent<PrototypeLootPanel>()?.Connect(this);
            foreach(var panel in GetComponentsInChildren<PrototypeModalPanel>(true))panel.Connect(this);
            _map.Clear();foreach(var b in Bindings)
            {
                if(b==null||string.IsNullOrEmpty(b.Id))continue;
                _map.Add(b.Id,b);string id=b.Id;
                if(b.Button!=null)b.Button.onClick.AddListener(()=>click(id));
                if(b.Slider!=null)b.Slider.onValueChanged.AddListener(v=>change(id,v));
            }
        }
        private void BindTooltipText()
        {
            if(TooltipTmpText==null&&Tooltip!=null)TooltipTmpText=Tooltip.GetComponentInChildren<TMP_Text>(true);
            if(TooltipTmpText==null)return;
            if(TooltipText!=null&&TooltipText.gameObject!=TooltipTmpText.gameObject)TooltipText.gameObject.SetActive(false);
            TooltipTmpText.raycastTarget=false;
        }
        public void SetTooltipText(string value)
        {
            if(TooltipTmpText!=null){if(TooltipTmpText.text!=value)TooltipTmpText.text=value;}
            else if(TooltipText!=null&&TooltipText.text!=value)TooltipText.text=value;
        }
        private void BindNoticeMessage()
        {
            var notice=Bindings.Find(b=>b!=null&&b.Id=="HUD.Notice");
            if(notice==null||notice.Rect==null)return;
            // 명시적 TMP 참조를 우선하고, 기존 씬은 Notice의 새 Message Text로 연결한다.
            var message=notice.TmpText;
            if(message==null)
            {
                var target=notice.Rect.Find("Message Text");
                if(target!=null)message=target.GetComponent<TMP_Text>();
            }
            if(message==null)return;
            if(notice.Text!=null&&notice.Text.gameObject!=message.gameObject)notice.Text.gameObject.SetActive(false);
            notice.TmpText=message;notice.Text=null;
        }
        public PrototypeUiBinding Item(string id)=>_map.TryGetValue(id,out var item)?item:null;
        // Unity에서 파괴된 오브젝트도 관리 참조는 남으므로 Rect를 Unity null 비교로 확인한다.
        public void Show(string id,bool show){var b=Item(id);if(b!=null&&b.Rect!=null&&b.Rect.gameObject.activeSelf!=show)b.Rect.gameObject.SetActive(show);}
        public void Label(string id,string value)
        {
            var b=Item(id);if(b==null)return;
            if(b.TmpText!=null){if(b.TmpText.text!=value)b.TmpText.text=value;}
            else if(b.Text!=null&&b.Text.text!=value)b.Text.text=value;
        }
        public void Enable(string id,bool enabled){var b=Item(id);if(b?.Button!=null)b.Button.interactable=enabled;}
        public bool Contains(string id,Vector2 screen)=>Item(id) is PrototypeUiBinding b&&b.Rect!=null&&b.Rect.gameObject.activeInHierarchy&&RectTransformUtility.RectangleContainsScreenPoint(b.Rect,screen);
        public bool Blocks(Vector2 screen)
        {
            if(EventSystem.current==null)return false;
            if(_pointer==null)_pointer=new PointerEventData(EventSystem.current);
            _pointer.position=screen;_hits.Clear();EventSystem.current.RaycastAll(_pointer,_hits);
            foreach(var h in _hits)if(h.gameObject!=null&&h.gameObject.transform.IsChildOf(transform))return true;return false;
        }
        private readonly Vector3[] _corners=new Vector3[4];
        public bool Guards(Vector2 screen)
        {
            if(Canvas==null)return false;
            foreach(var rect in CameraGuards)
            {
                if(rect==null||!rect.gameObject.activeInHierarchy)continue;rect.GetWorldCorners(_corners);
                float pad=32*Canvas.scaleFactor;
                if(screen.x>=_corners[0].x-pad&&screen.x<=_corners[2].x+pad&&screen.y>=_corners[0].y-pad&&screen.y<=_corners[2].y+pad)return true;
            }
            return false;
        }
    }
}
