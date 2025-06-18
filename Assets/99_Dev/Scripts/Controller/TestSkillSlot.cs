using System.Collections;
using UnityEngine;

namespace Afterlife.Controller
{
    public class TestSkillUI : MonoBehaviour
    {
        public Sprite IconSprite;
        public UI.Stage.SkillSlot SkillSlot;
        public float CooldownDuration = 5f;

        void Start()
        {
            SkillSlot.OnSkillSlotClickedEvent += OnSkillUsing;
        }

        void OnSkillUsing()
        {
            Debug.Log("Use skill!");
        }

        public void OnEnableButtonClicked()
        {
            SkillSlot.SetEnabled(true);
        }

        public void OnDisableButtonClicked()
        {
            SkillSlot.SetEnabled(false);
        }

        public void OnSetIconButtonClicked()
        {
            SkillSlot.SetIcon(IconSprite);
        }

        public void OnStartCooldownButtonClicked()
        {
            StopAllCoroutines();
            StartCoroutine(CooldownRoutine(CooldownDuration));
        }

        IEnumerator CooldownRoutine(float duration)
        {
            SkillSlot.ShowCooldown();
            var elapsedTime = duration;
            while (elapsedTime > 0f)
            {
                elapsedTime -= Time.deltaTime;
                SkillSlot.SetCooldownRatio(elapsedTime / duration);
                yield return null;
            }
            SkillSlot.SetCooldownRatio(0f);
            SkillSlot.HideCooldown();
        }

        public void OnStopCooldownButtonClicked()
        {
            StopAllCoroutines();
            SkillSlot.HideCooldown();
        }
    }
}