using System;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class Skill : View
    {
        [SerializeField] Transform containerTransform;
        [SerializeField] SkillSlot skillSlotPrefab;

        public event Action<int, SkillSlot> OnSkillSlotClickedEvent;

        public void ClearSkillSlots()
        {
            foreach (Transform child in containerTransform)
            {
                Destroy(child.gameObject);
            }
        }

        public SkillSlot GenerateSkillSlot()
        {
            var index = containerTransform.childCount;

            var skillSlot = Instantiate(skillSlotPrefab, containerTransform);
            skillSlot.OnSkillSlotClickedEvent += () => OnSkillSlotClickedEvent?.Invoke(index, skillSlot);

            return skillSlot;
        }
    }
}