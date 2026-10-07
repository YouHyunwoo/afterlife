using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Modules.Progression
{
    [CreateAssetMenu(menuName="Afterlife/Progression/Skills")]
    public sealed class SkillCatalog : ScriptableObject
    {
        public List<SkillData> Entries = new List<SkillData>();
    }
}
