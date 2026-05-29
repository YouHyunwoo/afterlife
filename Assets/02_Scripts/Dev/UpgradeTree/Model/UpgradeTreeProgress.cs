using System.Collections.Generic;

namespace Afterlife.Dev.UpgradeTree.Model
{
    public class UpgradeTreeProgress
    {
        private readonly Dictionary<string, int> _acquiredLevelCounts = new();

        public IReadOnlyDictionary<string, int> AcquiredLevelCounts => _acquiredLevelCounts;

        public int GetAcquiredLevelCount(string nodeId)
        {
            if (string.IsNullOrEmpty(nodeId)) { return 0; }
            return _acquiredLevelCounts.TryGetValue(nodeId, out var levelCount) ? levelCount : 0;
        }

        public void SetAcquiredLevelCount(string nodeId, int levelCount)
        {
            if (string.IsNullOrEmpty(nodeId)) { return; }

            if (levelCount <= 0)
            {
                _acquiredLevelCounts.Remove(nodeId);
                return;
            }

            _acquiredLevelCounts[nodeId] = levelCount;
        }

        public int IncrementAcquiredLevelCount(string nodeId)
        {
            var nextLevelCount = GetAcquiredLevelCount(nodeId) + 1;
            SetAcquiredLevelCount(nodeId, nextLevelCount);
            return nextLevelCount;
        }

        public void Clear()
        {
            _acquiredLevelCounts.Clear();
        }
    }
}
