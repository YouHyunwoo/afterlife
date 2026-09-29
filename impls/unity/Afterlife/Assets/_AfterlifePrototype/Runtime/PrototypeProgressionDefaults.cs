using System.Collections.Generic;
using System.Linq;
using Afterlife.Modules.Progression;
using UnityEngine;

namespace Afterlife.Prototype
{
    // 게임 고유 콘텐츠. 독립 모듈은 이 ID나 수치에 의존하지 않는다.
    public static class PrototypeProgressionDefaults
    {
        public const string ResourcePath = "AfterlifePrototype/Progression";
        public static List<string> Validate(ProgressionCatalog catalog, bool mvp = false)
        {
            var errors = catalog.Validate();
            if (errors.Count > 0) return errors;
            if (mvp ? catalog.Trees.Count != 1 || catalog.Trees[0].Id != "research"
                : catalog.Trees.Count != 2 || !catalog.Trees.Any(tree => tree.Id == "research") || !catalog.Trees.Any(tree => tree.Id == "altar")) errors.Add("강화 트리 구성과 게임 모드를 확인하세요.");
            foreach (string id in new[] { "lightning", "abundance", "ritual" })
                if (!catalog.Skills.Any(skill => skill.Id == id)) errors.Add("기본 스킬 ID가 필요합니다: " + id);
            string[] supported = { "housing", "work", "radius", "education", "construction", "structure", "physical", "interval", "territory", "cooperation", "lightning", "abundance", "shield", "chain", "shock", "echo", "healing", "harvest", "blessedShield", "ritual", "fieldSlot", "slotRecharge" };
            foreach (var node in catalog.Nodes)
            {
                if (!string.IsNullOrEmpty(node.EffectId) && !supported.Contains(node.EffectId)) errors.Add(node.Id + ": 게임 어댑터에 없는 효과 ID입니다: " + node.EffectId);
                if (node.Levels.Any(level => level.Value < 0)) errors.Add(node.Id + ": 효과 값은 0 이상이어야 합니다.");
                if ((node.EffectId == "construction" || node.EffectId == "interval") && node.Levels.Any(level => level.Value > .9f)) errors.Add(node.Id + ": 감소 비율 상한은 90%입니다.");
            }
            if (catalog.Nodes.Where(node => !string.IsNullOrEmpty(node.SkillId)).GroupBy(node => node.SkillId).Any(group => group.Count() > 1)) errors.Add("하나의 스킬 해금은 한 노드에서 관리해야 합니다.");
            return errors;
        }
        public static void Populate(ProgressionCatalog catalog)
        {
            var research = new UpgradeTreeData { Id = "research", Title = "제단 기초", Laboratory = false };
            var altar = new UpgradeTreeData { Id = "altar", Title = "제단 마법" };
            catalog.Trees = new List<UpgradeTreeData> { research, altar };
            Add(research, "R1", "영력 반경 확장", "영력 필드의 작업 반경이 직접 증가합니다.", -2, 1, "housing", new[]{.1f,.2f,.3f});
            Add(research, "R2", "작업 도구 개량", "채집·건설·수리의 작업 효율이 증가합니다.", 0, 2, "work", new[]{.08f,.16f,.24f,.32f,.4f});
            Add(research, "R3", "작업 반경 확장", "영력 필드의 작업 반경이 증가합니다.", 2, 1, "radius", new[]{.05f,.1f,.15f});
            Add(research, "R4", "작업 숙련", "영력 필드의 작업 효율이 직접 증가합니다.", -3, 3, "education", new[]{.1f,.2f,.3f,.4f,.5f}, "R1");
            Add(research, "R5", "표준 건축 공법", "건설에 필요한 작업량이 줄어듭니다.", -1, 4, "construction", new[]{.1f,.2f,.3f}, "R2");
            Add(research, "R6", "강화 골조", "건물의 최대 내구도가 증가합니다. 남아 있는 내구도의 비율은 유지됩니다.", -1, 6, "structure", new[]{.15f,.3f,.45f}, "R5");
            Add(research, "R7", "타격 도구 강화", "영력 필드의 물리 공격 피해가 증가합니다.", 4, 2, "physical", new[]{.1f,.2f,.3f,.4f,.5f}, "R3");
            Add(research, "R8", "필드 동조", "영력 필드의 자동 작업 간격을 줄입니다.", 2, 4, "interval", new[]{.05f,.1f,.15f}, "R2", "R3");
            Add(research, "R9", "영력 범위 확장", "영력 필드의 작업 반경이 직접 증가합니다.", -4, 5, "radius", new[]{.1f,.2f,.3f}, "R1", "R5");
            Add(research, "R10", "협동 작업 체계", "필드 안에 작업 대상이 2개 이상이면 채집·건설·수리 효율이 증가합니다.", 1, 7, "cooperation", new[]{.1f,.2f,.3f}, "R4", "R8");
            Add(altar, "A1", "낙뢰", "[Q] 커서 안의 무작위 적 하나에게 번개를 내립니다. 수치는 기본 주문 피해에 곱하는 배율입니다.", -2, 1, "lightning", new[]{1f,1.5f,2});
            Add(altar, "A2", "풍요의 기도", "[W] 10초 동안 채집과 자원 재생이 빨라집니다.", 0, -2, "abundance", new[]{.3f,.45f,.6f});
            Add(altar, "A3", "수호의 불꽃", "밤이 시작되면 건물에 최대 내구도에 비례한 보호막을 부여합니다. 밤이 끝나면 사라집니다.", 2, 1, "shield", new[]{.05f,.1f,.15f});
            Add(altar, "A4", "연쇄 번개", "번개가 주변의 다른 적에게도 이어집니다. 추가 대상은 원래 피해의 50%를 받습니다.", -4, 3, "chain", new[]{1f,2,3}, "A1");
            Add(altar, "A5", "감전 낙인", "번개에 맞은 적은 3초 동안 물리 피해를 더 받습니다.", -1, 3, "shock", new[]{.1f,.2f,.3f}, "A1");
            Add(altar, "A6", "잔향의 축복", "풍요의 기도가 끝나도 10초 동안 효과의 일부가 남습니다. 수치는 유지되는 효과의 비율입니다.", 0, -4, "echo", new[]{.25f,.4f,.55f}, "A2");
            Add(altar, "A7", "영혼의 보살핌", "수호 보호막이 남은 건물은 매초 최대 내구도의 일부를 회복합니다.", 4, 3, "healing", new[]{.002f,.004f,.006f}, "A3");
            Add(altar, "A8", "영혼 수확", "번개로 적을 처치하면 풍요의 기도 대기시간이 줄어듭니다. 한 번의 낙뢰·의식당 최대 3초 줄어듭니다.", -4, -2, "harvest", new[]{.5f,1,1.5f}, "A4", "A2");
            Add(altar, "A9", "축복받은 결계", "풍요의 기도 사용 시 최대 내구도에 비례한 보호막을 10초간 부여합니다. 수호 보호막과 중첩되지 않습니다.", 3, -3, "blessedShield", new[]{.03f,.06f,.09f}, "A6", "A3");
            Add(altar, "A10", "천벌의 의식", "[E] 6초 동안 2초마다 커서 안에 번개를 내립니다. 연쇄·감전도 적용되며, 수치는 주문 피해 배율입니다.", -3, 5, "ritual", new[]{.6f,.8f,1f}, "A4", "A5");
            altar.Nodes.Find(node => node.Id == "A1").SkillId = "lightning";
            altar.Nodes.Find(node => node.Id == "A2").SkillId = "abundance";
            altar.Nodes.Find(node => node.Id == "A10").SkillId = "ritual";
            catalog.Skills = new List<SkillData>
            {
                new SkillData { Id = "lightning", Title = "낙뢰", Cooldown = 12 },
                new SkillData { Id = "abundance", Title = "풍요의 기도", Cooldown = 60, Duration = 10 },
                new SkillData { Id = "ritual", Title = "천벌의 의식", Cooldown = 60, Duration = 6, PulseInterval = 2 }
            };
        }
        private static void Add(UpgradeTreeData tree, string id, string title, string description, int x, int y, string effect, float[] values, params string[] parents)
        {
            var node = new UpgradeNodeData { Id = id, Title = title, Description = description, Grid = new Vector2Int(x, y), EffectId = effect, Parents = new List<string>(parents) };
            switch (effect)
            {
                case "chain": node.ValueSuffix = "명"; break;
                case "harvest": node.ValueSuffix = "초"; break;
                case "lightning": case "ritual": node.ValueSuffix = "배"; break;
                default: node.ValueFormat = "P1"; break;
            }
            for (int index = 0; index < values.Length; index++)
                node.Levels.Add(new UpgradeLevelData { Cost = ((30 + parents.Length * 25) * (index + 1) + 29) / 30, Value = values[index] });
            tree.Nodes.Add(node);
        }
    }
}
