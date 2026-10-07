using System;
using System.Linq;

namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        public void NotifyAltarResearch() => Say("제단 또는 성소를 지으면 HUD에서 강화 트리를 열 수 있습니다.");
        private bool InsideVillage(Body body) => Territory.Contains(TileKey((int)Math.Round(body.Position.X), (int)Math.Round(body.Position.Z)));
        private Body AutomaticAttackTarget()
        {
            var houses = Bodies.Where(b => b.Alive && (b.Kind == Kind.House || b.Kind == Kind.TownHall) && b.Complete).ToArray();
            if (houses.Length == 0) return null;
            return Bodies.Where(b => b.Alive && b.Hostile && InsideVillage(b))
                .OrderBy(b => houses.Min(h => Point.Distance(h.Position, b.Position))).ThenBy(b => b.Id).FirstOrDefault();
        }
        private bool HasAutomaticSkillTarget(string id)
        {
            if (id != "abundance") return AutomaticAttackTarget() != null;
            return DivineFields.Fields.Any(f => Bodies.Any(b => b.Alive && (b.Kind == Kind.Tree || b.Kind == Kind.Rock)
                && b.DistanceTo(new Point(f.X, f.Z)) <= f.Radius));
        }
        private void TickAutomaticSkills()
        {
            if (!Settings.MvpSlots || Skills == null || Frozen || !HasAltar) return;
            Skills.TryActivate("lightning");
            Skills.TryActivate("abundance");
            Skills.TryActivate("ritual");
        }
        public bool CanUseProgressionNode(string id)
        {
            if (!_progressionRules.TryGetValue(id, out var rule)) return false;
            return CanUseTree(rule.TreeId);
        }
    }
}
