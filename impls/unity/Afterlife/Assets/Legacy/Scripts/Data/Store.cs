using System;
using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "StoreData", menuName = "Afterlife/Data/Store")]
    public class Store : ScriptableObject
    {
        public string Name;
        public StoreCategory[] StoreCategories;
    }

    [Serializable]
    public class StoreCategory
    {
        public string Name;
        public StoreItem[] StoreItems;
    }

    [Serializable]
    public class StoreItem
    {
        public string Id;
        public StoreStepItem[] StoreStepItems;
    }

    [Serializable]
    public class StoreStepItem
    {
        public Sprite Icon;
        public string Name;
        public string Description;
        public int Cost;
    }
}