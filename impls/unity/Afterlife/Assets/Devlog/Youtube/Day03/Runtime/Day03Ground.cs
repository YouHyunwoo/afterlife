using Afterlife.Modules.Terrain;
namespace Afterlife.Devlog.Day03
{
    public sealed class Day03Ground : IVillageGround
    {
        private readonly MapData _map;
        private readonly (float X,float Z)[] _resources;
        public Day03Ground(MapData map,(float X,float Z)[] resources){_map=map;_resources=resources;}
        public bool IsLand(int x,int z)=>_map!=null&&_map.At(x,z)!=TerrainBiome.Water;
        public bool HasResource(int x,int z)
        {
            foreach(var node in _resources){float dx=x-node.X,dz=z-node.Z;if(dx*dx+dz*dz<.64f)return true;}
            return false;
        }
    }
}
