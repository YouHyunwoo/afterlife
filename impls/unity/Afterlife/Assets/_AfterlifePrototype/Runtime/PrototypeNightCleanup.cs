using System;
using System.Linq;
using Afterlife.Modules.NightCleanup;

namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        public NightCleanupClock NightCleanup { get; } = new NightCleanupClock();
        public bool NightOvertime => Phase == Phase.Night && Remaining <= 0;
        private float _cleanupProbeTime;
        private int _cleanupProbeId;
        private void ResetNightCleanup()
        {
            NightCleanup.Reset(); _cleanupProbeTime = 0; _cleanupProbeId = 0;
        }
        private void TickNightCleanup(float dt)
        {
            NightCleanup.Tick(NightOvertime, dt);
            if (!NightOvertime) { _cleanupProbeTime = 0; return; }
            _cleanupProbeTime += dt;
            if (_cleanupProbeTime < 5) return;
            _cleanupProbeTime = 0;
            // One probe per interval. Search-budget exhaustion is unknown, not failure.
            var remaining = Bodies.Where(b => b.Alive && b.Raider).OrderBy(b => b.Id).ToArray();
            if (remaining.Length == 0) return;
            var enemy = remaining.FirstOrDefault(b => b.Id > _cleanupProbeId) ?? remaining[0];
            _cleanupProbeId = enemy.Id;
            if (enemy.StunRemaining > 0 || enemy.PendingTarget != 0) return;
            var targets = Bodies.Where(b => b.Alive && (b.Friendly || b.Building)).ToArray();
            if (targets.Length == 0 || CanReachCleanupTarget(enemy, targets)) return;
            foreach (var home in targets.Where(b => IsHousing(b.Kind)).OrderBy(b => b.DistanceTo(enemy.Position)))
            {
                for (int ring = 0; ring < 6; ring++) for (int i = 0; i < 24; i++)
                {
                    double angle = i * Math.PI / 12;
                    float radius = Math.Max(home.Width, home.Depth) * .5f + 1.5f + ring;
                    var point = new Point(home.Position.X + (float)Math.Cos(angle) * radius,
                        home.Position.Z + (float)Math.Sin(angle) * radius);
                    if (!Walkable(point, .4f) || !UnitFree(point, enemy.Id)) continue;
                    var route = Pathfinder.Find(point, home.Position, home, .8f, this, enemy.Id).ToArray();
                    if (route.Length == 0 || home.DistanceTo(route[route.Length - 1]) > .85f) continue;
                    enemy.Position = point; enemy.Route.Clear(); enemy.RouteTimer = 0;
                    enemy.FiringTarget = 0; enemy.StuckTime = 0;
                    Say("길이 막힌 잔여 적이 마을 주변으로 이동했습니다.");
                    return;
                }
            }
            // Withdrawal does not award a kill, loot or experience.
            Bodies.Remove(enemy);
            Say("접근할 수 없는 잔여 적이 퇴각했습니다.");
        }
        private bool CanReachCleanupTarget(Body enemy, Body[] targets)
        {
            Point Position(int x, int z) => new Point(enemy.Position.X + x * .5f, enemy.Position.Z + z * .5f);
            float range = AttackRange(enemy.Kind);
            var result = GridReachability.Search(
                (x, z) => Walkable(Position(x, z), .34f),
                (x, z) => targets.Any(t => t.DistanceTo(Position(x, z)) <= range && ClearAttack(Position(x, z), t, enemy.Id)));
            return result != Reachability.Unreachable;
        }
    }
}
