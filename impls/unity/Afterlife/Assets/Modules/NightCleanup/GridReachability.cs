using System;
using System.Collections.Generic;

namespace Afterlife.Modules.NightCleanup
{
    public enum Reachability { Reachable, Unreachable, Unknown }

    // Finite walkable grid supplied by the game. Exhausting a budget must never trigger recovery.
    public static class GridReachability
    {
        public static Reachability Search(Func<int, int, bool> walkable,
            Func<int, int, bool> canEngage, int budget = 4096)
        {
            if (walkable == null || canEngage == null) throw new ArgumentNullException();
            if (!walkable(0, 0)) return Reachability.Unreachable;
            var frontier = new Queue<(int x, int z)>();
            var visited = new HashSet<(int x, int z)> { (0, 0) };
            frontier.Enqueue((0, 0));
            for (int count = 0; frontier.Count > 0; count++)
            {
                if (count >= budget) return Reachability.Unknown;
                var cell = frontier.Dequeue();
                if (canEngage(cell.x, cell.z)) return Reachability.Reachable;
                for (int x = -1; x <= 1; x++) for (int z = -1; z <= 1; z++)
                {
                    if (x == 0 && z == 0) continue;
                    var key = (cell.x + x, cell.z + z);
                    if (!visited.Add(key) || !walkable(key.Item1, key.Item2)) continue;
                    if (x != 0 && z != 0 && (!walkable(key.Item1, cell.z) || !walkable(cell.x, key.Item2)))
                    {
                        // The same cell may still be entered later without crossing this corner.
                        visited.Remove(key); continue;
                    }
                    frontier.Enqueue(key);
                }
            }
            return Reachability.Unreachable;
        }
    }
}
