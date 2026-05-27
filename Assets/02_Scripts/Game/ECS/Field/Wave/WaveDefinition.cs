namespace Afterlife.Dev.Game
{
    public class WaveDefinition
    {
        public WaveEntry[] Entries { get; }
        public float SpawnInterval { get; }
        public int EdgeMargin { get; }

        public WaveDefinition(WaveEntry[] entries, float spawnInterval, int edgeMargin = 4)
        {
            Entries = entries;
            SpawnInterval = spawnInterval;
            EdgeMargin = edgeMargin;
        }
    }
}