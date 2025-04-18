using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "FieldData", menuName = "Afterlife/Data/Field")]
    public class Field : ScriptableObject
    {
        public int VillageCount;
        public GameObject VillagePrefab;
    }
}