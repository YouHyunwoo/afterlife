using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace Afterlife.Devlog.Cinematics
{
    // 게임 카메라 출력만 흐리게 처리한다. TMP는 블러 위에 독립적으로 렌더링한다.
    public sealed class FilmCredits : IDisposable
    {
        private readonly GameObject _root;
        private readonly RawImage _background;
        private readonly Image _shade;
        private readonly RectTransform _roll;
        private readonly TextMeshProUGUI _text;
        private readonly Camera _camera;
        private readonly ShowcaseFilm _film;
        private readonly Material _blur;
        private RenderTexture _horizontal,_vertical,_ownedCapture;
        private bool _active;
        private float _amount;

        public FilmCredits(Transform frame,Camera camera,ShowcaseFilm film)
        {
            _camera=camera; _film=film;
            _root=new GameObject("FilmCredits",typeof(RectTransform),typeof(RectMask2D));
            _root.transform.SetParent(frame,false); Stretch(_root.GetComponent<RectTransform>());
            var background=new GameObject("BlurredWorld",typeof(RectTransform),typeof(RawImage));
            background.transform.SetParent(_root.transform,false);
            _background=background.GetComponent<RawImage>();_background.raycastTarget=false;Stretch(_background.rectTransform);
            var shade=new GameObject("CreditsShade",typeof(RectTransform),typeof(Image));shade.transform.SetParent(_root.transform,false);
            _shade=shade.GetComponent<Image>();_shade.raycastTarget=false;Stretch(shade.GetComponent<RectTransform>());
            var roll=new GameObject("CreditsRoll",typeof(RectTransform),typeof(TextMeshProUGUI));roll.transform.SetParent(_root.transform,false);
            _roll=roll.GetComponent<RectTransform>();_roll.anchorMin=_roll.anchorMax=new Vector2(.5f,0);_roll.pivot=new Vector2(.5f,0);
            _text=roll.GetComponent<TextMeshProUGUI>();_text.font=film.SubtitleFont;_text.fontSize=44;
            _text.alignment=TextAlignmentOptions.Center;_text.color=Color.white;_text.raycastTarget=false;_text.richText=true;
            _text.text=film.CreditsText;_text.textWrappingMode=TextWrappingModes.Normal;
            if (film.CreditsBlurShader!=null) _blur=new Material(film.CreditsBlurShader){name="FilmCreditsBlur"};
            RenderPipelineManager.endCameraRendering+=Rendered;
            _root.SetActive(false);
        }
        private static void Stretch(RectTransform rect)
        {rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}

        public void Evaluate(bool active,float time,float duration)
        {
            if (active!=_active)
            {
                _active=active;_root.SetActive(active);
                if (!active) ReleaseTargets();
            }
            if (!active) return;
            if (_camera.targetTexture==null)
            {
                _ownedCapture=new RenderTexture(1280,720,24,RenderTextureFormat.DefaultHDR){name="CreditsWorldCapture"};
                _ownedCapture.Create();_camera.targetTexture=_ownedCapture;
            }
            _amount=Mathf.SmoothStep(0,1,Mathf.Clamp01(time/Mathf.Max(.1f,_film.CreditsFadeSeconds)));
            _background.color=new Color(1,1,1,_ownedCapture!=null ? 1 : _amount);
            _shade.color=new Color(0,0,0,_amount*_film.CreditsDarkness);
            var frame=(RectTransform)_root.transform;
            float height=Mathf.Max(1,frame.rect.height);
            if (_text.text!=_film.CreditsText) _text.text=_film.CreditsText;
            float scale=Mathf.Clamp(_film.CreditsScale,.5f,2.5f);
            _roll.localScale=Vector3.one*scale;
            _roll.sizeDelta=new Vector2(Mathf.Max(1,frame.rect.width*.8f/scale),height*.55f);
            float rollHeight=Mathf.Max(height*.55f,_text.preferredHeight+48);
            _roll.sizeDelta=new Vector2(_roll.sizeDelta.x,rollHeight);
            // 아래 바깥에서 등장하고 위 바깥으로 완전히 빠진다. 정지/탐색도 영상 시각을 따른다.
            float progress=Mathf.Clamp01((time-.75f)/Mathf.Max(.1f,duration-2));
            _roll.anchoredPosition=new Vector2(0,Mathf.Lerp(-rollHeight*scale-height*.07f,height*1.03f,progress));
        }
        private void Rendered(ScriptableRenderContext context,Camera camera)
        {
            if (!_active || camera!=_camera || camera.targetTexture==null) return;
            var source=camera.targetTexture;
            if (_horizontal==null)
            {
                _horizontal=new RenderTexture(640,360,0,RenderTextureFormat.DefaultHDR){name="CreditsBlurHorizontal",filterMode=FilterMode.Bilinear};_horizontal.Create();
                _vertical=new RenderTexture(640,360,0,RenderTextureFormat.DefaultHDR){name="CreditsBlurVertical",filterMode=FilterMode.Bilinear};_vertical.Create();
                _background.texture=_vertical;
            }
            var previous=RenderTexture.active;
            try
            {
                if (_blur==null) {Graphics.Blit(source,_vertical);return;}
                // 픽셀 카메라의 Point 필터를 그대로 샘플링하지 않고 Bilinear 중간 화면에서 처리한다.
                Graphics.Blit(source,_vertical);
                _blur.SetFloat("_Sigma",Mathf.Clamp(_amount*_film.CreditsBlurRadius*1.2f,0,16));
                _blur.SetVector("_Direction",new Vector4(1f/_vertical.width,0,0,0));
                Graphics.Blit(_vertical,_horizontal,_blur);
                _blur.SetVector("_Direction",new Vector4(0,1f/_horizontal.height,0,0));
                Graphics.Blit(_horizontal,_vertical,_blur);
            }
            finally {RenderTexture.active=previous;}
        }
        private void ReleaseTargets()
        {
            _background.texture=null;
            if (_ownedCapture!=null && _camera!=null && _camera.targetTexture==_ownedCapture) _camera.targetTexture=null;
            Release(ref _ownedCapture);Release(ref _horizontal);Release(ref _vertical);
        }
        private static void Release(ref RenderTexture target)
        {if(target==null)return;target.Release();UnityEngine.Object.Destroy(target);target=null;}
        public void Dispose()
        {
            RenderPipelineManager.endCameraRendering-=Rendered;ReleaseTargets();
            if(_blur!=null)UnityEngine.Object.Destroy(_blur);
            _root.SetActive(false);UnityEngine.Object.Destroy(_root);
        }
    }
}
