using UnityEngine;

namespace Afterlife.Dev.Field
{
    [CreateAssetMenu(fileName = "New Building", menuName = "Afterlife/Building")]
    public class BuildingData : ScriptableObject
    {
        public Vector2Int Size;
    }
}