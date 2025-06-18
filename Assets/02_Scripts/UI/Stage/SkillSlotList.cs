using System;
using UnityEngine;

namespace Afterlife.UI.Stage
{
    public class SkillSlotList : UI.View
    {
        public SkillSlot SkillSlotPrefab;

        public event Action<int, SkillSlot> OnSkillSlotClickedEvent;

        public void ClearSkillSlots()
        {
            foreach (Transform child in transform)
            {
                Destroy(child.gameObject);
            }
        }

        public SkillSlot GenerateSkillSlot()
        {
            var index = transform.childCount;

            var skillSlot = Instantiate(SkillSlotPrefab, transform);
            skillSlot.OnSkillSlotClickedEvent += () => OnSkillSlotClickedEvent?.Invoke(index, skillSlot);

            return skillSlot;
        }
    }
}