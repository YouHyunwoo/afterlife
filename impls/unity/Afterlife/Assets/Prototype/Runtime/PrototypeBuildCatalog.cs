using System.Collections.Generic;
using UnityEngine;
namespace Afterlife.Prototype
{
    [CreateAssetMenu(menuName="Afterlife/Build Catalog")]
    public sealed class PrototypeBuildCatalog : ScriptableObject
    {
        public const string ResourcePath="Data/BuildCatalog";
        public PrototypeBuildItem ItemPrefab;
        public List<PrototypeBuildingData> Buildings=new List<PrototypeBuildingData>();
    }
}
