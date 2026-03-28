using System.Collections.Generic;

namespace Afterlife.Dev.World
{
    public class WorldRepository
    {
        private readonly Dictionary<string, World> models = new();

        public bool Save(World world)
        {
            if (world == null || world.Id == null || world.Id == "") return false;
            models.Add(world.Id, world);
            return true;
        }

        public bool Delete(World world)
        {
            if (world == null || world.Id == null || world.Id == "") return false;
            if (!models.ContainsKey(world.Id)) return false;

            return models.Remove(world.Id);
        }

        public bool FindWorldById(string id, out World world)
        {
            world = null;
            if (id == null || id == "" || !models.ContainsKey(id)) return false;
            world = models[id];
            return true;
        }
    }
}