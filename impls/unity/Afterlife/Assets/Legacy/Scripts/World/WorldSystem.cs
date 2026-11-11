using System;
using System.Linq;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class WorldSystem
    {
        public event Action<World> OnWorldGenerated;
        public event Func<World, Awaitable> OnWorldGeneratedAsync;

        public bool GenerateWorld(WorldMapGenerationParameter @params, out World world)
        {
            world = null;

            var generator = new WorldMapGenerator();
            if (!generator.Generate(@params, out var worldMap))
                return false;

            var worldId = System.Guid.NewGuid().ToString();
            world = new World(worldId, worldMap);

            OnWorldGenerated?.Invoke(world);

            return true;
        }

        public async Awaitable<World> GenerateWorldAsync(WorldMapGenerationParameter @params)
        {
            var generator = new WorldMapGenerator();
            if (!generator.Generate(@params, out var worldMap))
                return null;

            var worldId = System.Guid.NewGuid().ToString();
            var world = new World(worldId, worldMap);

            if (OnWorldGeneratedAsync != null)
            {
                var handlers = OnWorldGeneratedAsync.GetInvocationList().Cast<Func<World, Awaitable>>();
                foreach (Func<World, Awaitable> handler in handlers)
                    await handler(world);
            }

            return world;
        }
    }
}