using System;
using System.Collections.Generic;
using System.Linq;
using Afterlife.Modules.Upgrades;
using Afterlife.Modules.Skills;

namespace Afterlife.Prototype
{
    public sealed class ProgressionRule
    {
        public string NodeId, TreeId, EffectId, SkillId;
        public bool RequiresAltar;
        public float[] Values;
    }
    // 게임 연결 계층. 독립 모듈 사이의 관계와 월드 효과는 이곳에서만 조립한다.
    public sealed partial class VillageModel
    {
        public UpgradeService Upgrades { get; private set; }
        public SkillService Skills { get; private set; }
        private readonly Dictionary<string, ProgressionRule> _progressionRules = new Dictionary<string, ProgressionRule>();
        private readonly Dictionary<string, float> _effects = new Dictionary<string, float>();
        private float _abundanceEcho, _ritualRefund;
        public event Action<Point, Point> LightningStruck;
        public float Effect(string id) => _effects.TryGetValue(id, out var value) ? value : 0;
        public float ProductionSpeed => 1 + (Skills != null && Skills.States.TryGetValue("abundance", out var state) && state.ActiveRemaining > 0 ? Effect("abundance") : _abundanceEcho > 0 ? Effect("abundance") * Effect("echo") : 0);
        public float WorkMultiplier => 1 + Effect("work");
        public float ConstructionWorkFactor => Math.Max(.1f, 1 - Effect("construction"));
        public float VillageRadius => Settings.HouseRadius * (1 + Effect("territory"));

        public void ConfigureProgression(IEnumerable<UpgradeDefinition> nodes, IEnumerable<SkillDefinition> skills, IEnumerable<ProgressionRule> rules)
        {
            if (Upgrades != null) throw new InvalidOperationException("강화 모듈은 씬 조립 시 한 번만 구성합니다.");
            foreach (var rule in rules) _progressionRules.Add(rule.NodeId, rule);
            Upgrades = new UpgradeService(nodes, new ProgressionWallet(this));
            Skills = new SkillService(skills, Settings.MvpSlots
                ? (ISkillActivationPolicy)new AutomaticSkillPolicy(() => !Frozen && HasAltar, HasAutomaticSkillTarget)
                : new SkillPolicy(this));
            Upgrades.Purchased += OnProgressionPurchased;
            Skills.Activated += OnSkillActivated;
            Skills.Pulsed += OnSkillPulsed;
            Skills.Expired += OnSkillExpired;
        }
        private sealed class ProgressionWallet : IUpgradeWallet
        {
            private readonly VillageModel _model;
            public ProgressionWallet(VillageModel model) { _model = model; }
            public bool TrySpend(int amount)
            {
                return _model.TrySpendEtheron(amount);
            }
        }
        private sealed class SkillPolicy : ISkillActivationPolicy
        {
            private readonly VillageModel _model;
            public SkillPolicy(VillageModel model) { _model = model; }
            public bool CanActivate(string id) => !_model.Frozen && _model.HasAltar &&
                (id == "abundance" || _model.PointerVisible && _model.Bodies.Any(body => body.Alive && body.Hostile && body.DistanceTo(_model.WorkCenter) <= _model.Radius));
        }
        public bool CanUseTree(string tree) => !Frozen && HasAltar;
        public PurchaseResult BuyProgression(string id)
        {
            if (Upgrades == null || !_progressionRules.TryGetValue(id, out var rule)) return PurchaseResult.UnknownNode;
            if (!CanUseProgressionNode(id)) { Say("제단 또는 성소가 필요합니다."); return PurchaseResult.Locked; }
            var result = Upgrades.Purchase(id);
            if (result != PurchaseResult.Success) Say(result == PurchaseResult.InsufficientFunds ? "에테론이 부족합니다." : "모든 부모 강화를 1단계 이상 구매해야 합니다.");
            return result;
        }
        private void OnProgressionPurchased(string id, int level)
        {
            float oldStructure = 1 + Effect("structure");
            var rule = _progressionRules[id];
            _effects.Clear();
            foreach (var item in _progressionRules.Values)
            {
                int rank = Upgrades.Level(item.NodeId);
                if (rank > 0 && item.Values.Length >= rank && !string.IsNullOrEmpty(item.EffectId))
                    _effects[item.EffectId] = Effect(item.EffectId) + item.Values[rank - 1];
            }
            if (!string.IsNullOrEmpty(rule.SkillId)) Skills.SetLevel(rule.SkillId, level);
            float structure = (1 + Effect("structure")) / oldStructure;
            foreach (var body in Bodies)
                if (body.Alive && body.Building) { body.MaxHealth *= structure; body.Health *= structure; body.SpiritShield *= structure; }
            Recalculate(); _pulseClock = Math.Min(_pulseClock, Interval);
            RefreshFieldSlots();
            Say("강화 " + id + " · " + level + "단계 적용");
        }
        private void ResetProgression()
        {
            _effects.Clear(); Skills?.Reset(); Upgrades?.Reset(); _abundanceEcho = _ritualRefund = 0;
        }
        private void TickProgression(float dt)
        {
            _abundanceEcho = Math.Max(0, _abundanceEcho - dt);
            Skills?.Tick(dt);
            TickAutomaticSkills();
            foreach (var body in Bodies)
            {
                body.ShockRemaining = Math.Max(0, body.ShockRemaining - dt);
                if (body.ShieldRemaining > 0)
                {
                    body.ShieldRemaining = Math.Max(0, body.ShieldRemaining - dt);
                    if (body.ShieldRemaining <= 0) { body.SpiritShield = 0; body.GuardianShield = false; }
                }
                if (body.Alive && body.GuardianShield && body.SpiritShield > 0)
                    body.Health = Math.Min(body.MaxHealth, body.Health + body.MaxHealth * Effect("healing") * dt);
            }
        }
        public bool TryCastSkill(string id, Point aim, bool valid)
        {
            if (Settings.MvpSlots || Skills == null || Frozen) return false;
            var previous = WorkCenter; bool visible = PointerVisible;
            WorkCenter = aim; PointerVisible = valid;
            try { return Skills.TryActivate(id); }
            finally { WorkCenter = previous; PointerVisible = visible; }
        }
        private void OnSkillActivated(string id)
        {
            if (id == "lightning") StrikeLightning(Effect("lightning"), false);
            else if (id == "ritual") _ritualRefund = 0;
            else if (id == "abundance")
            {
                _abundanceEcho = 0;
                GrantShields(Effect("blessedShield"), Rules.Combat.AbundanceShieldSeconds, false);
                Say("풍요의 기도 · 채집과 자원 재생 가속");
            }
        }
        private void OnSkillPulsed(string id) { if (id == "ritual") StrikeLightning(Effect("ritual"), true); }
        private void OnSkillExpired(string id) { if (id == "abundance" && Effect("echo") > 0) _abundanceEcho = Rules.Combat.EchoSeconds; }
        private void GrantShields(float ratio, float seconds, bool guardian)
        {
            if (ratio <= 0) return;
            foreach (var body in Bodies)
                if (body.Alive && body.Building && body.Complete && !body.MovingOut)
                {
                    float amount = body.MaxHealth * ratio;
                    if (amount <= body.SpiritShield) continue;
                    body.SpiritShield = amount; body.ShieldRemaining = seconds; body.GuardianShield = guardian;
                }
        }
        private void ClearNightShields()
        {
            foreach (var body in Bodies) { body.SpiritShield = body.ShieldRemaining = 0; body.GuardianShield = false; }
        }
        private void StrikeLightning(float multiplier, bool ritual)
        {
            if (!Settings.MvpSlots && !PointerVisible) return;
            var targets = Settings.MvpSlots ? new[] { AutomaticAttackTarget() }.Where(body => body != null).ToArray()
                : Bodies.Where(body => body.Alive && body.Hostile && body.DistanceTo(WorkCenter) <= Radius).ToArray();
            if (targets.Length == 0) return;
            var first = targets[_random.Next(targets.Length)];
            var hit = new HashSet<int>(); var target = first; var origin = Settings.MvpSlots ? first.Position : WorkCenter; int kills = 0;
            for (int hop = 0; target != null && hop <= (int)Effect("chain"); hop++)
            {
                hit.Add(target.Id);
                // 주문은 물리 연구 수치와 분리된 기본 피해를 사용한다.
                float damage = Rules.Combat.LightningDamage * multiplier * (hop == 0 ? 1 : Rules.Combat.ChainDamageMultiplier);
                LightningStruck?.Invoke(origin, target.Position);
                ApplyDamage(target, damage); EmitText(target, "번개 " + damage.ToString("0.#"), FeedbackKind.Lightning);
                if (!target.Alive) kills++;
                else { target.ShockRemaining = Rules.Combat.ShockSeconds; target.ShockMultiplier = Effect("shock"); }
                origin = target.Position;
                target = Bodies.Where(body => body.Alive && body.Hostile && !hit.Contains(body.Id) && (!Settings.MvpSlots || InsideVillage(body)) && Point.Distance(body.Position, origin) <= Rules.Combat.ChainRange)
                    .OrderBy(body => Point.Distance(body.Position, origin)).ThenBy(body=>body.Id).FirstOrDefault();
            }
            float refund = Math.Min(ritual ? Math.Max(0, Rules.Combat.HarvestRefundCap - _ritualRefund) : Rules.Combat.HarvestRefundCap, kills * Effect("harvest"));
            Skills.ReduceCooldown("abundance", refund);
            if (ritual) _ritualRefund += refund;
        }
        private float CooperationMultiplier()
        {
            if (Effect("cooperation") <= 0) return 1;
            int count = 0;
            Point center = _activeDivineFieldCenter ?? WorkCenter;
            float radius = _activeDivineFieldCenter.HasValue ? _activeDivineFieldRadius : Radius;
            foreach (var body in Bodies)
                if (body.Alive && body.DistanceTo(center) <= radius &&
                    (body.Kind == Kind.Tree || body.Kind == Kind.Rock || body.Building && (!body.Complete || body.Health < body.MaxHealth && Money > 0)))
                    if (++count >= 2) return 1 + Effect("cooperation");
            return 1;
        }
    }
}
