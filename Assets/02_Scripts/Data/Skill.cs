using System;
using UnityEngine;

namespace Afterlife.Data
{
    [Serializable]
    public class SkillParameterGroup
    {
        public string Key;
        public string Value;
    }

    [CreateAssetMenu(fileName = "SkillData", menuName = "Afterlife/Data/Skill")]
    public class Skill : ScriptableObject
    {
        public string Id;
        public Sprite IconSprite;
        public float ActiveDuration;
        public float CooldownDuration;
        public SkillParameterGroup[] SkillParameterGroups;
    }
}