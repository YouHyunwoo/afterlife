using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Building Data")]
    public sealed class PrototypeBuildingData : ScriptableObject
    {
        public Kind Id;
        public string DisplayName;
        public Sprite Icon;
        [Min(0)] public int GoldCost;
        public bool RequiresBuilding;
        public Kind RequiredBuilding=Kind.Altar;
        public ConstructionRule ToRule()=>new ConstructionRule{Id=Id,GoldCost=GoldCost,RequiresBuilding=RequiresBuilding,RequiredBuilding=RequiredBuilding};
    }
}
