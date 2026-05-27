using UnityEngine;

namespace Afterlife.Dev.Game
{
    [CreateAssetMenu(fileName = "New Resource", menuName = "Afterlife/ResourceData")]
    public class ResourceData : PlaceableData
    {
        public int Woods;
        public int Stones;
        public float HarvestSpeed;
    }
}
