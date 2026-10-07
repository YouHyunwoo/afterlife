using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Afterlife.Prototype
{
    // Button.onClick은 interactable=false일 때 호출되지 않으므로 거절 피드백만 별도로 받는다.
    public sealed class PrototypeDisabledButtonSound : MonoBehaviour, IPointerDownHandler
    {
        private Button _button;
        private PrototypeAudio _audio;
        public void Bind(Button button, PrototypeAudio audio) { _button=button;_audio=audio; }
        public void OnPointerDown(PointerEventData eventData)
        {
            if(eventData.button==PointerEventData.InputButton.Left&&_button!=null&&_button.enabled&&
                !_button.interactable&&_audio!=null)_audio.RequestSound(SoundCue.Error);
        }
    }
}
