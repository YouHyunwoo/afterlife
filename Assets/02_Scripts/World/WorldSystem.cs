using System;
using System.Linq;
using UnityEngine;

namespace Afterlife.Dev.World
{
    public class WorldSystem
    {
        public event Action<GenerationResult> OnWorldGenerated;
        public event Func<GenerationResult, Awaitable> OnWorldGeneratedAsync;

        public bool GenerateWorld(WorldMapGenerationParameter @params, out GenerationResult result)
        {
            result = null;

            var generator = new WorldMapGenerator();
            if (!generator.Generate(@params, out result))
                return false;

            OnWorldGenerated?.Invoke(result);

            return true;
        }

        public async Awaitable<GenerationResult> GenerateWorldAsync(WorldMapGenerationParameter @params)
        {
            var generator = new WorldMapGenerator();
            if (!generator.Generate(@params, out var world))
                return world;

            if (OnWorldGeneratedAsync != null)
            {
                var handlers = OnWorldGeneratedAsync.GetInvocationList().Cast<Func<GenerationResult, Awaitable>>();
                foreach (Func<GenerationResult, Awaitable> handler in handlers)
                    await handler(world);
            }

            return world;
        }
    }
}