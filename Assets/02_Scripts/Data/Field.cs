using UnityEngine;

namespace Afterlife.Data
{
    [System.Serializable]
    public class ResourceObjectGroup
    {
        public string Name;
        public int Count;
        public GameObject Prefab;
    }

    [CreateAssetMenu(fileName = "FieldData", menuName = "Afterlife/Data/Field")]
    public class Field : ScriptableObject
    {
        [Header("Village")]
        public int VillageCount;
        public GameObject VillagePrefab;

        [Header("Resources")]
        public ResourceObjectGroup[] ResourceObjectGroups;
    }
}