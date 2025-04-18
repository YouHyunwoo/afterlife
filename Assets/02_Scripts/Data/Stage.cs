using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "StageData", menuName = "Afterlife/Data/Stage")]
    public class Stage : ScriptableObject
    {
        public Map MapData;
    }
}