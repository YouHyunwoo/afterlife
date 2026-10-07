using UnityEngine;
namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private float _uiSize=1,_edgeFraction=.25f,_cameraSpeedFactor=1,_noticeTime=-10;
        // 이전 QA 스크립트가 참조하는 상태. 실제 레이아웃은 씬 RectTransform을 사용한다.
        private Vector2 _modalScroll;
        private float HudScale=>Mathf.Max(.5f,Mathf.Min(2.4f,Screen.width/1200f,Screen.height/675f)*_uiSize);
        private float HudWidth=>Screen.width/HudScale;
        private float HudHeight=>Screen.height/HudScale;
        private Rect BuildToggle=>new Rect(16,158,64,68);
        private Rect LootToggle=>new Rect(16,238,64,68);
        private Rect ToolsRect=>new Rect(92,158,300,Mathf.Min(492,HudHeight-174));
        private Rect MenuButton=>new Rect(HudWidth-80,16,64,60);
        private Rect CameraButton=>new Rect(HudWidth-156,16,64,60);
        private void SaveUiPreferences()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if(_qaFolder!=null)return;
#endif
            PrototypeUiPreferences.Save(new PrototypeUiPreferenceValues(_uiSize,_edgeFraction,_cameraSpeedFactor));
        }
        private void DisposeUi() { }
    }
}
