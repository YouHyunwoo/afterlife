using UnityEngine;
using Afterlife.Modules.Terrain;
namespace Afterlife.Modules.SceneFeatures
{
    [CreateAssetMenu(menuName="Afterlife/Map Data")]
    public sealed class MapAsset : ScriptableObject
    {
        public MapData Data=new MapData(7);
    }
}
