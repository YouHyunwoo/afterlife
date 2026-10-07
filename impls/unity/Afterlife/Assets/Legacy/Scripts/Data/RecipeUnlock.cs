using UnityEngine;

namespace Afterlife.Data
{
    [CreateAssetMenu(fileName = "RecipeUnlockData", menuName = "Afterlife/Data/Recipe Unlock")]
    public class RecipeUnlock : ScriptableObject
    {
        public string Id;
        public string ItemId;
        public int Cost;
        public string[] PrerequisiteRecipeIds;
    }
}
