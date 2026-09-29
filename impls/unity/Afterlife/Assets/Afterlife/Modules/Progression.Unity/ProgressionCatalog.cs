using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Modules.Upgrades;
using Afterlife.Modules.Skills;
using UnityEngine;

namespace Afterlife.Modules.Progression
{
    [Serializable] public sealed class UpgradeLevelData
    {
        [Min(0)] public int Cost = 30;
        public float Value;
    }
    [Serializable] public sealed class UpgradeNodeData
    {
        public string Id;
        public string Title;
        public Sprite Icon;
        [TextArea] public string Description;
        public Vector2Int Grid;
        public List<string> Parents = new List<string>();
        public string EffectId;
        public string SkillId;
        public bool RequiresAltar;
        public string ValueFormat = "0.##";
        public string ValueSuffix = "";
        public List<UpgradeLevelData> Levels = new List<UpgradeLevelData>();
        public UpgradeDefinition ToDefinition() => new UpgradeDefinition(Id, Parents, Levels.Select(level => level.Cost));
        public float Value(int level) => level <= 0 || Levels.Count == 0 ? 0 : Levels[Math.Min(level, Levels.Count) - 1].Value;
        public string FormatValue(int level) => ValueFormat == "P1"
            ? (Value(level) * 100).ToString("0.#") + "%" + ValueSuffix
            : Value(level).ToString(ValueFormat) + ValueSuffix;
    }
    [Serializable] public sealed class UpgradeTreeData
    {
        public string Id;
        public string Title;
        public bool Laboratory;
        public List<UpgradeNodeData> Nodes = new List<UpgradeNodeData>();
    }
    [Serializable] public sealed class SkillData
    {
        public string Id;
        public string Title;
        [Min(0)] public float Cooldown;
        [Min(0)] public float Duration;
        [Min(0)] public float PulseInterval;
        public SkillDefinition ToDefinition() => new SkillDefinition(Id, Cooldown, Duration, PulseInterval);
    }
    [CreateAssetMenu(menuName = "Afterlife/Progression Catalog")]
    public sealed class ProgressionCatalog : ScriptableObject
    {
        public List<UpgradeTreeAsset> TreeAssets = new List<UpgradeTreeAsset>();
        public SkillCatalog SkillCatalog;
        [SerializeField,HideInInspector,UnityEngine.Serialization.FormerlySerializedAs("Trees")]
        private List<UpgradeTreeData> _legacyTrees = new List<UpgradeTreeData>();
        [SerializeField,HideInInspector,UnityEngine.Serialization.FormerlySerializedAs("Skills")]
        private List<SkillData> _legacySkills = new List<SkillData>();
        public List<UpgradeTreeData> Trees { get => TreeAssets.Count>0 ? TreeAssets.Select(asset=>asset.Data).ToList() : _legacyTrees; set { TreeAssets.Clear(); _legacyTrees=value; } }
        public List<SkillData> Skills { get => SkillCatalog!=null ? SkillCatalog.Entries : _legacySkills; set { SkillCatalog=null; _legacySkills=value; } }
        public ScriptableObject TreeOwner(int index) => TreeAssets.Count>0 ? (ScriptableObject)TreeAssets[index] : this;
        public IEnumerable<ScriptableObject> DataAssets => new ScriptableObject[]{this}.Concat(TreeAssets).Concat(SkillCatalog!=null?new ScriptableObject[]{SkillCatalog}:Array.Empty<ScriptableObject>());
        public ProgressionCatalog RuntimeCopy()
        {
            var result=Instantiate(this);
            // 참조된 트리도 복사하여 플레이 중 에셋 변경이 런타임 구매 정의와 어긋나지 않게 한다.
            result._legacyTrees=Trees.Select(tree=>JsonUtility.FromJson<UpgradeTreeData>(JsonUtility.ToJson(tree))).ToList();
            result._legacySkills=Skills.Select(skill=>JsonUtility.FromJson<SkillData>(JsonUtility.ToJson(skill))).ToList();
            result.TreeAssets.Clear();result.SkillCatalog=null;
            return result;
        }
        public IEnumerable<UpgradeNodeData> Nodes => Trees.SelectMany(tree => tree.Nodes);
        public List<string> Validate()
        {
            var errors = new List<string>();
            try
            {
                if (Trees.Any(tree => string.IsNullOrWhiteSpace(tree.Id)) || Trees.Select(tree => tree.Id).Distinct().Count() != Trees.Count) errors.Add("트리 ID는 비어 있거나 중복될 수 없습니다.");
                if (Nodes.Select(node => node.Id).Distinct().Count() != Nodes.Count()) errors.Add("강화 ID가 중복됩니다.");
                foreach (var tree in Trees)
                {
                    if (tree.Nodes.GroupBy(node => node.Grid).Any(group => group.Count() > 1) || tree.Nodes.Any(node => node.Grid == Vector2Int.zero)) errors.Add(tree.Title + ": 격자가 겹치거나 중심을 차지합니다.");
                    foreach (var node in tree.Nodes)
                    {
                        _ = node.FormatValue(1);
                        if (node.Parents.Any(parent => !tree.Nodes.Any(item => item.Id == parent))) errors.Add(node.Id + ": 같은 트리의 부모만 연결할 수 있습니다.");
                        if (!string.IsNullOrEmpty(node.SkillId) && !Skills.Any(skill => skill.Id == node.SkillId)) errors.Add(node.Id + ": 스킬 ID를 찾을 수 없습니다.");
                        if (node.Levels.Any(level => float.IsNaN(level.Value) || float.IsInfinity(level.Value))) errors.Add(node.Id + ": 효과 값이 유효하지 않습니다.");
                    }
                }
                _ = new UpgradeService(Nodes.Select(node => node.ToDefinition()), new ValidationWallet());
                _ = new SkillService(Skills.Select(skill => skill.ToDefinition()), new ValidationPolicy());
            }
            catch (Exception exception) { errors.Add(exception.Message); }
            return errors;
        }
        private sealed class ValidationWallet : IUpgradeWallet { public bool TrySpend(int amount) => false; }
        private sealed class ValidationPolicy : ISkillActivationPolicy { public bool CanActivate(string id) => false; }
    }
}
