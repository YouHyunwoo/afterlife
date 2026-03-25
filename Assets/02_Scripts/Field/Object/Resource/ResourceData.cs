using UnityEngine;

namespace Afterlife.Dev.Field
{
    [CreateAssetMenu(fileName = "New Resource", menuName = "Afterlife/Resource")]
    public class ResourceData : ObjectData
    {
        public int Woods;
        public int Stones;
        public HoldableVisible ResourceHoldableVisiblePrefab;
    }
}