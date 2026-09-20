using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
namespace Afterlife.Prototype
{
    public enum ModalBackdropMode { Dark, DarkBlur }
    [DisallowMultipleComponent]
    public sealed class PrototypeModalBackdrop : MonoBehaviour
    {
        public CanvasGroup ModalLayer;
        public RawImage Background;
        public Shader BlurShader;
        public ModalBackdropMode Mode=ModalBackdropMode.DarkBlur;
        [Tooltip("모달 한 층의 암전 농도. 모달이 쌓이면 검은 배경을 겹친 것처럼 누적됩니다.")]
        [Range(0,1)] public float Darkness=.6f;
        [Tooltip("화면 픽셀 기준 블러 샘플 반경. 클수록 넓게 흐려집니다.")]
        [Range(0,24)] public float BlurRadius=8;
        [Range(1,4)] public int Downsample=2;
        private bool _visible,_captured;
        private ModalBackdropMode _renderMode;
        private bool _useBlur;
        private bool? _requestedBlur;
        private Coroutine _capture;
        private RenderTexture _source,_horizontal,_blurred;
        private Material _material;
        private float _lastRadius=-1,_lastDarkness=-1;
        private int _lastDownsample;
        private int _depth;
        private float EffectiveDarkness=>1-Mathf.Pow(1-Mathf.Clamp01(Darkness),_depth);
        private RectTransform _top;
        private CanvasGroup _hiddenTop;
        private float _topAlpha;
        private Camera _captureCamera;
        private readonly WaitForEndOfFrame _endOfFrame=new WaitForEndOfFrame();
        public void SetVisible(bool visible,RectTransform top=null,int depth=1,bool? blur=null)
        {
            // 깊이만 바뀌면 기존 캡처 텍스처를 재사용하고 암전 농도만 갱신한다.
            _depth=visible?Mathf.Max(0,depth):0;
            _requestedBlur=blur;
            bool useBlur=blur??Mode==ModalBackdropMode.DarkBlur;
            if(_visible==visible&&_top==top&&_useBlur==useBlur)return;
            ResetBackdrop();_top=top;_visible=visible;_renderMode=Mode;_useBlur=useBlur;
            if(!visible){ResetBackdrop();return;}
            // 씬 편집으로 CanvasGroup이 제거되어도 저장된 모달 바인딩에서 복구한다.
            if(ModalLayer==null)
            {
                var ui=GetComponent<PrototypeCanvas>();
                var layer=ui!=null?ui.Item("ModalLayer")?.Rect:null;
                if(layer!=null)
                {
                    ModalLayer=layer.GetComponent<CanvasGroup>();
                    if(ModalLayer==null)ModalLayer=layer.gameObject.AddComponent<CanvasGroup>();
                }
            }
            if(Background==null||ModalLayer==null)return;
            Background.transform.SetAsLastSibling();
            if(_top!=null)_top.SetAsLastSibling();
            if(BlurShader==null)BlurShader=Resources.Load<Shader>("UI/ModalBackdropBlur");
            if(!_useBlur||BlurShader==null||!BlurShader.isSupported){ShowDark();return;}
            if(_material==null)_material=new Material(BlurShader){hideFlags=HideFlags.HideAndDontSave};
            ShowDark();
            _capture=StartCoroutine(CaptureBackground());
        }
        private IEnumerator CaptureBackground()
        {
            while(_visible)
            {
                yield return _endOfFrame;
                if(!_visible)break;
                if(!_useBlur){ShowDark();continue;}
                bool failed=false;
                try
                {
                    int width=Mathf.Max(1,Screen.width),height=Mathf.Max(1,Screen.height);
                    if(_source==null||_source.width!=width||_source.height!=height)
                    {
                        Release(ref _source);_source=CreateTexture(width,height,"Live modal background",24);
                    }
                    CaptureUi();_captured=true;RenderBlur();
                }
                catch(System.Exception error){_captured=false;ShowDark();Debug.LogException(error,this);failed=true;}
                if(failed)break;
            }
            _capture=null;
        }
        private void CaptureUi()
        {
            var canvas=GetComponent<Canvas>();
            if(canvas==null)throw new System.InvalidOperationException("모달 블러 Canvas가 없습니다.");
            if(_captureCamera==null)
            {
                var go=new GameObject("Modal UI Capture Camera"){hideFlags=HideFlags.HideAndDontSave};
                _captureCamera=go.AddComponent<Camera>();_captureCamera.enabled=false;
                // 실제 월드와 떨어진 좁은 프러스텀에는 이 Canvas만 배치한다.
                go.transform.position=new Vector3(10000,10000,-10000);
                _captureCamera.nearClipPlane=.1f;_captureCamera.farClipPlane=2;
                _captureCamera.clearFlags=CameraClearFlags.SolidColor;_captureCamera.backgroundColor=Color.black;
                _captureCamera.allowHDR=false;_captureCamera.allowMSAA=false;
                var data=_captureCamera.GetUniversalAdditionalCameraData();data.renderPostProcessing=false;data.renderShadows=false;
            }
            var mode=canvas.renderMode;var camera=canvas.worldCamera;float distance=canvas.planeDistance;
            bool backgroundEnabled=Background.enabled;
            try
            {
                Background.enabled=false;
                if(_top!=null)
                {
                    _hiddenTop=_top.GetComponent<CanvasGroup>();
                    if(_hiddenTop==null)_hiddenTop=_top.gameObject.AddComponent<CanvasGroup>();
                    _topAlpha=_hiddenTop.alpha;_hiddenTop.alpha=0;
                }
                _captureCamera.targetTexture=_source;
                canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=_captureCamera;canvas.planeDistance=1;
                Canvas.ForceUpdateCanvases();
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=_source};
                if(!RenderPipeline.SupportsRenderRequest(_captureCamera,request))throw new System.InvalidOperationException("현재 렌더러가 UI 캡처 요청을 지원하지 않습니다.");
                RenderPipeline.SubmitRenderRequest(_captureCamera,request);
            }
            finally
            {
                canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=distance;
                _captureCamera.targetTexture=null;
                RestoreTop();Background.enabled=backgroundEnabled;Canvas.ForceUpdateCanvases();
            }
        }
        private void LateUpdate()
        {
            if(!_visible||Background==null)return;
            if(Mode!=_renderMode){var top=_top;int depth=_depth;var blur=_requestedBlur;SetVisible(false);SetVisible(true,top,depth,blur);return;}
            if(_capture!=null)return;
            if(!_useBlur){ShowDark();return;}
            if(!_captured){ShowDark();return;}
            if(_lastRadius!=BlurRadius||_lastDarkness!=EffectiveDarkness||_lastDownsample!=Mathf.Clamp(Downsample,1,4)||Background.texture!=_blurred)RenderBlur();
        }
        private void ShowDark(){Background.texture=Texture2D.whiteTexture;Background.color=new Color(0,0,0,EffectiveDarkness);if(ModalLayer!=null)ModalLayer.alpha=1;}
        private void RenderBlur()
        {
            int down=Mathf.Clamp(Downsample,1,4),w=Mathf.Max(1,_source.width/down),h=Mathf.Max(1,_source.height/down);
            if(_horizontal==null||_horizontal.width!=w||_horizontal.height!=h)
            {
                Release(ref _horizontal);Release(ref _blurred);
                _horizontal=CreateTexture(w,h,"Modal blur horizontal");_blurred=CreateTexture(w,h,"Modal blur result");
            }
            var previous=RenderTexture.active;
            try
            {
                _material.SetVector("_Direction",new Vector4(BlurRadius/_source.width,0,0,0));_material.SetFloat("_Darkness",0);
                // 전용 카메라의 RT는 스크린샷과 달리 이미 Unity 텍스처 방향이다.
                _material.SetFloat("_FlipY",0);
                Graphics.Blit(_source,_horizontal,_material);
                _material.SetFloat("_FlipY",0);
                _material.SetVector("_Direction",new Vector4(0,BlurRadius/_source.height,0,0));_material.SetFloat("_Darkness",EffectiveDarkness);
                Graphics.Blit(_horizontal,_blurred,_material);
            }
            finally{RenderTexture.active=previous;}
            Background.texture=_blurred;Background.color=Color.white;
            _lastRadius=BlurRadius;_lastDarkness=EffectiveDarkness;_lastDownsample=down;
        }
        private static RenderTexture CreateTexture(int width,int height,string label,int depth=0)
        {
            var texture=new RenderTexture(width,height,depth,RenderTextureFormat.ARGB32){name=label,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};texture.Create();return texture;
        }
        private void ResetBackdrop()
        {
            if(_capture!=null){StopCoroutine(_capture);_capture=null;}
            RestoreTop();
            if(Background!=null)Background.enabled=true;
            if(ModalLayer!=null)ModalLayer.alpha=1;
            if(Background!=null)Background.texture=null;
            _captured=false;Release(ref _source);Release(ref _horizontal);Release(ref _blurred);
        }
        private void RestoreTop(){if(_hiddenTop!=null)_hiddenTop.alpha=_topAlpha;_hiddenTop=null;}
        private static void Release(ref RenderTexture texture){if(texture==null)return;texture.Release();Destroy(texture);texture=null;}
        private void OnDisable(){_visible=false;ResetBackdrop();}
        private void OnDestroy(){ResetBackdrop();if(_material!=null)Destroy(_material);if(_captureCamera!=null)Destroy(_captureCamera.gameObject);}
    }
}
