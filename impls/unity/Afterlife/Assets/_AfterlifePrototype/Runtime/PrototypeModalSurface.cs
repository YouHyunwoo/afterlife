using UnityEngine;
namespace Afterlife.Prototype
{
    public enum ModalPresentation { Window, Screen }
    [DisallowMultipleComponent]
    public sealed class PrototypeModalSurface : MonoBehaviour
    {
        public ModalPresentation Presentation=ModalPresentation.Window;
        [Tooltip("켜면 이 모달이 스택에 있는 동안 게임 진행을 멈춥니다. 끄면 모달 뒤에서도 게임이 진행됩니다.")]
        public bool PauseGameplay=false;
        [Header("배경 효과")]
        [InspectorName("배경 블러")]
        [Tooltip("켜면 모달 뒤를 흐리게 표시합니다. 스택에 블러를 켠 모달이 하나라도 있으면 유지됩니다.")]
        public bool BlurBackground=true;
        [InspectorName("배경 어둡게")]
        [Tooltip("켜면 배경 암전에 한 층을 추가합니다. 체크한 모달 수만큼 암전이 누적됩니다.")]
        public bool DarkenBackground=true;
        private void OnEnable()
        {
            if(Presentation!=ModalPresentation.Screen)return;
            var rect=(RectTransform)transform;
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
