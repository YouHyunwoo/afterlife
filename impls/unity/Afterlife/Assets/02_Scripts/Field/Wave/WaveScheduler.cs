using Moonstone.Ore;

namespace Afterlife.Dev.Field
{
    public class WaveSchedulerConfig
    {
        public EnemyData GoblinData;
        public EnemyVisible GoblinPrefab;
        public EnemyData OrcData;
        public EnemyVisible OrcPrefab;
        public EnemyData TrollData;
        public EnemyVisible TrollPrefab;
    }

    public static class WaveScheduler
    {
        // 트리거 시간: 이전 웨이브 시작 + 진행 시간 + 60s 휴식
        // Wave 1: 30s  (~16s 진행) → Wave 2: 106s (~14s 진행) → Wave 3: 180s (~17s 진행)
        // → Wave 4: 257s (~20s 진행) → Wave 5: 337s
        private static readonly float[] TriggerTimes = { 30f, 106f, 180f, 257f, 337f };

        public static void Schedule(EventSystem eventSystem, Container container, WaveSchedulerConfig config)
        {
            var definitions = BuildWaveDefinitions(config);

            for (var i = 0; i < definitions.Length; i++)
            {
                var waveEvent = new WaveEvent(TriggerTimes[i], definitions[i]);
                container.Inject(waveEvent);
                eventSystem.Register(waveEvent);
            }
        }

        private static WaveDefinition[] BuildWaveDefinitions(WaveSchedulerConfig c)
        {
            return new WaveDefinition[]
            {
                // Wave 1: 예산 10 — 고블린 7(7) + 오크 1(3) = 10
                new(new WaveEntry[]
                {
                    new(c.GoblinData, c.GoblinPrefab, 7),
                    new(c.OrcData,    c.OrcPrefab,    1),
                }, spawnInterval: 2.0f),

                // Wave 2: 예산 20 — 고블린 5(5) + 오크 3(9) = 14
                new(new WaveEntry[]
                {
                    new(c.GoblinData, c.GoblinPrefab, 5),
                    new(c.OrcData,    c.OrcPrefab,    3),
                }, spawnInterval: 1.8f),

                // Wave 3: 예산 40 — 고블린 4(4) + 오크 6(18) + 트롤 1(8) = 30
                new(new WaveEntry[]
                {
                    new(c.GoblinData, c.GoblinPrefab, 4),
                    new(c.OrcData,    c.OrcPrefab,    6),
                    new(c.TrollData,  c.TrollPrefab,  1),
                }, spawnInterval: 1.5f),

                // Wave 4: 예산 80 — 고블린 6(6) + 오크 8(24) + 트롤 3(24) = 54
                new(new WaveEntry[]
                {
                    new(c.GoblinData, c.GoblinPrefab, 6),
                    new(c.OrcData,    c.OrcPrefab,    8),
                    new(c.TrollData,  c.TrollPrefab,  3),
                }, spawnInterval: 1.2f),

                // Wave 5: 예산 160 — 고블린 8(8) + 오크 12(36) + 트롤 8(64) = 108
                new(new WaveEntry[]
                {
                    new(c.GoblinData, c.GoblinPrefab, 8),
                    new(c.OrcData,    c.OrcPrefab,    12),
                    new(c.TrollData,  c.TrollPrefab,  8),
                }, spawnInterval: 1.0f),
            };
        }
    }
}
