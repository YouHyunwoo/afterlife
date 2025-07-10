using System;
using UnityEngine;

namespace Afterlife.Data
{
    public enum ItemType
    {
        Consumable,
        Equipment,
        Construction,
        Miscellaneous
    }

    [Serializable]
    public class ItemEffect
    {
        public string EffectType;
        public float[] Values;
    }

    [Serializable]
    public class CraftRequirement
    {
        public string ItemId;
        public int Amount;
    }

    [CreateAssetMenu(fileName = "ItemData", menuName = "Afterlife/Data/Item")]
    public class Item : ScriptableObject
    {
        public string Id;
        public ItemType Type;
        public Sprite Icon;
        public ItemEffect[] Effects;
        public CraftRequirement[] CraftRequirements;
        public GameObject ConstructionPrefab;
        public GameObject PreviewPrefab;
    }
}