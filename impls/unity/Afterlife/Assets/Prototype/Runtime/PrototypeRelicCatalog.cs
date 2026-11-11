using System;
using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    [Serializable]
    public sealed class PrototypeRelicData
    {
        public RelicKind Id;
        public string DisplayName;
        public Sprite Icon;
        [TextArea] public string TooltipTemplate="{name} · {level}단계\n{effect}";
        public string Tooltip(int level, RelicRules effects=null)=>TooltipTemplate.Replace("{name}",DisplayName).Replace("{level}",level.ToString()).Replace("{effect}",(effects??new RelicRules()).Describe(Id,level));
    }
    [CreateAssetMenu(menuName="Afterlife/Relic Catalog")]
    public sealed class PrototypeRelicCatalog : ScriptableObject
    {
        public const string ResourcePath="Data/RelicCatalog";
        public RectTransform ListItemPrefab;
        public List<PrototypeRelicData> Entries=new List<PrototypeRelicData>();
        public PrototypeRelicData Find(RelicKind id)=>Entries.Find(entry=>entry!=null&&entry.Id==id);
        private void OnValidate()
        {
            var ids=new HashSet<RelicKind>();
            foreach(var entry in Entries)if(entry!=null&&!ids.Add(entry.Id))Debug.LogError($"유물 ID 중복: {entry.Id}",this);
        }
    }
}
