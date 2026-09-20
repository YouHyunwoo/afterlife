namespace Afterlife.Dev.World
{
    public class World : Moonstone.Ore.Model
    {
        private WorldMap _worldMap;

        public WorldMap WorldMap => _worldMap;

        public World(string id, WorldMap worldMap) : base(id)
        {
            _worldMap = worldMap;
        }
    }
}