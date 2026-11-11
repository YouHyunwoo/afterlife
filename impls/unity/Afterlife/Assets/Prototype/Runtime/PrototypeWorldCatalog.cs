using System;
using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    [Serializable] public sealed class PrototypeWorldAsset { public string Id; public GameObject Prefab; }
    [CreateAssetMenu(menuName="Afterlife/Data/World Visuals")]
    public sealed class PrototypeWorldCatalog : ScriptableObject
    {
        public List<PrototypeWorldAsset> Entries=new List<PrototypeWorldAsset>();
        public GameObject Find(string id)=>Entries.Find(entry=>entry.Id==id)?.Prefab;
    }
}
