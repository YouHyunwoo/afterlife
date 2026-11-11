using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Afterlife.Modules.Upgrades
{
    public interface IUpgradeWallet { bool TrySpend(int amount); }
    public enum PurchaseResult { Success, UnknownNode, Hidden, Locked, MaximumLevel, InsufficientFunds }

    public sealed class UpgradeDefinition
    {
        public string Id { get; }
        public IReadOnlyList<string> Parents { get; }
        public IReadOnlyList<int> Costs { get; }
        public int MaxLevel => Costs.Count;
        public UpgradeDefinition(string id, IEnumerable<string> parents, IEnumerable<int> costs)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("강화 ID가 필요합니다.");
            Id = id;
            Parents = Array.AsReadOnly((parents ?? Array.Empty<string>()).ToArray());
            Costs = Array.AsReadOnly((costs ?? Array.Empty<int>()).ToArray());
            if (Costs.Count == 0 || Costs.Any(cost => cost < 0)) throw new ArgumentException("유효한 단계별 가격이 필요합니다.");
            if (Parents.Distinct().Count() != Parents.Count) throw new ArgumentException("중복 부모입니다.");
        }
    }

    // 순수 C# 모듈: 재화, 게임 효과, Unity 오브젝트를 소유하지 않는다.
    public sealed class UpgradeService
    {
        private readonly Dictionary<string, UpgradeDefinition> _nodes;
        private readonly Dictionary<string, int> _levels = new Dictionary<string, int>();
        private readonly IUpgradeWallet _wallet;
        private bool _purchasing;
        public event Action<string, int> Purchased;
        public event Action Changed;
        public IReadOnlyDictionary<string, UpgradeDefinition> Nodes { get; }
        public UpgradeService(IEnumerable<UpgradeDefinition> definitions, IUpgradeWallet wallet)
        {
            _wallet = wallet ?? throw new ArgumentNullException(nameof(wallet));
            _nodes = definitions.ToDictionary(node => node.Id, StringComparer.Ordinal);
            Nodes = new ReadOnlyDictionary<string, UpgradeDefinition>(_nodes);
            var visited = new HashSet<string>();
            foreach (var id in _nodes.Keys) Validate(id, new HashSet<string>(), visited);
        }
        private void Validate(string id, HashSet<string> path, HashSet<string> visited)
        {
            if (!_nodes.ContainsKey(id)) throw new ArgumentException("존재하지 않는 부모: " + id);
            if (visited.Contains(id)) return;
            if (!path.Add(id)) throw new ArgumentException("강화 트리에 순환 연결이 있습니다: " + id);
            foreach (var parent in _nodes[id].Parents) Validate(parent, path, visited);
            path.Remove(id); visited.Add(id);
        }
        public int Level(string id) => _levels.TryGetValue(id, out var level) ? level : 0;
        public bool Visible(string id) => _nodes.TryGetValue(id, out var node) && (node.Parents.Count == 0 || node.Parents.Any(parent => Level(parent) > 0));
        public bool Unlocked(string id) => _nodes.TryGetValue(id, out var node) && node.Parents.All(parent => Level(parent) > 0);
        public int NextCost(string id) => _nodes.TryGetValue(id, out var node) && Level(id) < node.MaxLevel ? node.Costs[Level(id)] : 0;
        public PurchaseResult Purchase(string id)
        {
            if (_purchasing) return PurchaseResult.Locked;
            if (!_nodes.TryGetValue(id, out var node)) return PurchaseResult.UnknownNode;
            if (!Visible(id)) return PurchaseResult.Hidden;
            if (!Unlocked(id)) return PurchaseResult.Locked;
            if (Level(id) >= node.MaxLevel) return PurchaseResult.MaximumLevel;
            _purchasing = true;
            try
            {
                if (!_wallet.TrySpend(NextCost(id))) return PurchaseResult.InsufficientFunds;
                _levels[id] = Level(id) + 1;
                Purchased?.Invoke(id, Level(id)); Changed?.Invoke();
                return PurchaseResult.Success;
            }
            finally { _purchasing = false; }
        }
        public void Reset() { _levels.Clear(); Changed?.Invoke(); }
    }
}
