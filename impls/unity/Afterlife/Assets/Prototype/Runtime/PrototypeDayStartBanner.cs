using UnityEngine;
using UnityEngine.UI;
using TMPro;
namespace Afterlife.Prototype
{
    [DisallowMultipleComponent,RequireComponent(typeof(RectTransform))]
    public sealed class PrototypeDayStartBanner : MonoBehaviour
    {
        [Header("표시 참조")]
        public Image Background;
        public Graphic Label;
        [Header("크기 · Canvas 기준")]
        [Min(0)] public float LineHeight=3;
        [Min(0)] public float ExpandedHeight=88;
        [Range(0,1)] public float ScreenWidthRatio=1;
        [Header("단계별 시간 · 닫기는 동일 시간 역재생")]
        [Min(.01f)] public float HorizontalSeconds=.2f;
        [Min(.01f)] public float VerticalSeconds=.2f;
        [Min(.01f)] public float TextFadeSeconds=.2f;
        [Min(0)] public float HoldSeconds=1;
        public AnimationCurve ExpansionCurve=AnimationCurve.EaseInOut(0,0,1,1);
        private RectTransform _rect;
        private CanvasGroup _textFade;
        private bool _playing;
        private float _elapsed;
        public bool IsPlaying=>_playing;
        public void ResolveReferences()
        {
            if(Background==null)Background=GetComponent<Image>();
            if(Label==null)
            {
                Label=GetComponentInChildren<TMP_Text>(true);
                if(Label==null)Label=GetComponentInChildren<Text>(true);
            }
        }
        private void Prepare()
        {
            ResolveReferences();_rect=(RectTransform)transform;
            _rect.anchorMin=_rect.anchorMax=_rect.pivot=new Vector2(.5f,.5f);
            _rect.anchoredPosition=Vector2.zero;
            if(Background!=null)Background.raycastTarget=false;
            if(Label!=null)
            {
                Label.raycastTarget=false;
                _textFade=Label.GetComponent<CanvasGroup>();
                if(_textFade==null)Debug.LogError("Day banner Label needs a CanvasGroup in the scene.",this);
                if(_textFade==null)return;
                _textFade.blocksRaycasts=false;_textFade.interactable=false;
            }
        }
        private void Awake(){Prepare();Apply(0,LineHeight,0);if(Background!=null)Background.enabled=false;}
        public void Play(int day)
        {
            Prepare();gameObject.SetActive(true);
            if(Label is TMP_Text tmp)tmp.text=$"{day}일차";
            else if(Label is Text legacy)legacy.text=$"{day}일차";
            if(Label!=null)Label.gameObject.SetActive(true);
            _elapsed=0;_playing=true;
            Apply(0,LineHeight,0);if(Background!=null)Background.enabled=true;
        }
        public void Hide()
        {
            _playing=false;_elapsed=0;
            if(_rect!=null)Apply(0,LineHeight,0);
            if(Background!=null)Background.enabled=false;
            gameObject.SetActive(false);
        }
        private void OnDisable()
        {
            _playing=false;_elapsed=0;
            if(_rect!=null)Apply(0,LineHeight,0);
            if(Background!=null)Background.enabled=false;
        }
        private void Update()
        {
            if(!_playing)return;
            _elapsed+=Time.unscaledDeltaTime;
            float horizontal=Mathf.Max(.01f,HorizontalSeconds),vertical=Mathf.Max(.01f,VerticalSeconds),fade=Mathf.Max(.01f,TextFadeSeconds);
            float opening=horizontal+vertical+fade,hold=Mathf.Max(0,HoldSeconds);
            if(_elapsed>=opening*2+hold){Hide();return;}
            // 닫기는 동일 타임라인을 거꾸로 평가해 중간 단계도 정확하게 역재생한다.
            float t=_elapsed<=opening?_elapsed:_elapsed<=opening+hold?opening:opening*2+hold-_elapsed;
            if(t<horizontal)Apply(Ease(t/horizontal),LineHeight,0);
            else if(t<horizontal+vertical)Apply(1,Mathf.Lerp(LineHeight,ExpandedHeight,Ease((t-horizontal)/vertical)),0);
            else Apply(1,ExpandedHeight,Mathf.Clamp01((t-horizontal-vertical)/fade));
        }
        private float Ease(float t)=>ExpansionCurve==null?Mathf.Clamp01(t):Mathf.Clamp01(ExpansionCurve.Evaluate(Mathf.Clamp01(t)));
        private void Apply(float width,float height,float alpha)
        {
            var parent=_rect.parent as RectTransform;
            float fullWidth=parent!=null?parent.rect.width:Screen.width;
            _rect.sizeDelta=new Vector2(fullWidth*Mathf.Clamp01(ScreenWidthRatio)*width,Mathf.Max(0,height));
            if(_textFade!=null)_textFade.alpha=alpha;
        }
    }
}
