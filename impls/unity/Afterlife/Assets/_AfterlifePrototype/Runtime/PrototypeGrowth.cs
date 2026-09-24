using System;
using System.Collections.Generic;
using System.Linq;
namespace Afterlife.Prototype
{
    public sealed partial class VillageModel
    {
        private readonly Dictionary<Upgrade,int> _combatUpgrades=new Dictionary<Upgrade,int>();
        public static readonly Upgrade[] ResearchUpgrades=(Upgrade[])Enum.GetValues(typeof(Upgrade));
        public int UpgradeMax(Upgrade upgrade)=>5;
        public bool UpgradeUnlocked(Upgrade upgrade)=>upgrade!=Upgrade.FireImpact&&upgrade!=Upgrade.BurnDamage&&upgrade!=Upgrade.BurnDuration||Level(Upgrade.Ignite)>0;
        public int ExtraLevel(Upgrade upgrade)=>_combatUpgrades.TryGetValue(upgrade,out int level)?level:upgrade==Upgrade.MultiTarget?1:0;
        public int TargetCount=>Level(Upgrade.MultiTarget);
        public float WallHealth=>260*(1+.25f*Level(Upgrade.WallHealth));
        public float IgniteChance=>.05f*Level(Upgrade.Ignite);
        public float FireImpact=>8*(1+.3f*Level(Upgrade.FireImpact));
        public float BurnDamagePerSecond=>3*(1+.4f*Level(Upgrade.BurnDamage));
        public float BurnSeconds=>3+Level(Upgrade.BurnDuration);
        public static bool IsTower(Kind kind)=>kind==Kind.ArrowTower||kind==Kind.MagicTower;
        public static bool HasBuildingLight(Kind kind)=>kind==Kind.House||kind==Kind.Research||kind==Kind.Barracks||kind==Kind.Market||kind==Kind.LargeTest;
        public float BodyVisionRadius(Body body)
        {
            if(!body.Alive)return 0;
            if(body.Friendly)return 5;
            if(!body.Building)return 0;
            if(HasBuildingLight(body.Kind))return Math.Max(3,Settings.VillageVision);
            if(body.Complete&&!body.MovingOut&&IsTower(body.Kind))return TowerRange(body)+.65f;
            return Math.Max(1.5f,Math.Max(3,Settings.VillageVision)/3);
        }
        public float BodyLightRadius(Body body)=>body.Alive&&body.Complete&&!body.MovingOut&&HasBuildingLight(body.Kind)?BodyVisionRadius(body)*.5f:0;
        public float TowerRange(Body tower)=>TowerRange(tower.Kind)+.35f*tower.TowerLevel;
        public float TowerDamage(Body tower)=>(tower.Kind==Kind.MagicTower?34:18)*(1+.2f*Level(tower.Kind==Kind.MagicTower?Upgrade.MagicDamage:Upgrade.ArrowDamage)+.2f*tower.TowerLevel);
        public int TowerPrice(Body tower)=>40*(tower.TowerLevel+1);
        public void UpgradeTower(int id)
        {
            var tower=Find(id);if(Frozen||tower==null||!IsTower(tower.Kind)||!tower.Complete||tower.MovingOut||tower.TowerLevel>=5||Money<TowerPrice(tower))return;
            Money-=TowerPrice(tower);tower.TowerLevel++;Say($"{Name(tower.Kind)} 강화 {tower.TowerLevel}/5 · 피해 {TowerDamage(tower):0.#} / 범위 {TowerRange(tower):0.##}");
        }
        public string UpgradeName(Upgrade u)
        {
            switch(u)
            {
                case Upgrade.Pulse:return "신력 필드 주기";case Upgrade.Residents:return "집당 주민";case Upgrade.Construction:return "자동 건설";
                case Upgrade.CriticalChance:return "커서 치명타 확률";case Upgrade.CriticalPower:return "커서 치명타 배율";
                case Upgrade.WallHealth:return "성벽 체력";case Upgrade.ArrowDamage:return "화살 타워 피해";case Upgrade.ArrowRange:return "화살 타워 범위";
                case Upgrade.MagicDamage:return "마법 타워 피해";case Upgrade.MagicRange:return "마법 타워 범위";case Upgrade.MultiTarget:return "타워 동시 공격";
                case Upgrade.Ignite:return "발화 확률";case Upgrade.FireImpact:return "발화 순간 피해";case Upgrade.BurnDamage:return "화상 지속 피해";case Upgrade.BurnDuration:return "화상 지속 시간";
                default:return u.ToString();
            }
        }
        public string UpgradeValue(Upgrade u)
        {
            int level=Level(u);
            switch(u)
            {
                case Upgrade.Pulse:return $"필드 작업 주기 {Interval:0.00}초 · 단계마다 ×0.84";case Upgrade.Residents:return $"집당 {ResidentsPerHouse}명 · 단계마다 +1";
                case Upgrade.Construction:return $"자동 속도 ×{1+.5f*level:0.0} · 단계마다 +0.5";
                case Upgrade.CriticalChance:return $"{CursorCriticalChance*100:0}% · 단계마다 +5%p";case Upgrade.CriticalPower:return $"×{CursorCriticalMultiplier:0.0} · 단계마다 +0.5";
                case Upgrade.WallHealth:return $"최대 {WallHealth:0} · 단계마다 기본 체력 +25%";
                case Upgrade.ArrowDamage:case Upgrade.MagicDamage:return $"기본 피해 ×{1+.2f*level:0.0} · 단계마다 +20%";
                case Upgrade.ArrowRange:case Upgrade.MagicRange:return $"연구 보너스 +{.5f*level:0.0} · 단계마다 +0.5타일";
                case Upgrade.MultiTarget:return $"동시 {TargetCount}대상 · 최대 5대상";
                case Upgrade.Ignite:return $"{IgniteChance*100:0}% · 단계마다 +5%p";case Upgrade.FireImpact:return $"발화 시 {FireImpact:0.#} · 단계마다 +30%";
                case Upgrade.BurnDamage:return $"초당 {BurnDamagePerSecond:0.#} · 단계마다 +40%";case Upgrade.BurnDuration:return $"{BurnSeconds:0}초 · 단계마다 +1초";
                default:return "";
            }
        }
        private void ResetCombatGrowth(){_combatUpgrades.Clear();_tacticsClock=0;_tacticsRevision=-1;}
        private void UpgradeWallHealth()
        {
            foreach(var wall in Bodies)if(wall.Alive&&wall.Kind==Kind.Wall){float ratio=wall.Health/Math.Max(1,wall.MaxHealth);wall.MaxHealth=WallHealth;wall.Health=wall.MaxHealth*ratio;}
        }
        private void PlayerHit(Body target,float damage,bool physical=true)
        {
            if(physical&&target!=null&&target.ShockRemaining>0)damage*=1+target.ShockMultiplier;
            ApplyDamage(target,damage);
            if(target==null||!target.Alive||!target.Hostile||IgniteChance<=0||_random.NextDouble()>=IgniteChance)return;
            ApplyDamage(target,FireImpact,false);
            EmitText(target,$"불 {FireImpact:0.#}",FeedbackKind.Fire);
            if(!target.Alive)return;
            // 화상은 중첩 폭증 대신 가장 강한 피해를 유지하고 남은 시간을 갱신한다.
            target.BurnRemaining=Math.Max(target.BurnRemaining,BurnSeconds);target.BurnDps=Math.Max(target.BurnDps,BurnDamagePerSecond);
        }
        private void ProcessBurns(float dt)
        {
            foreach(var body in Bodies)
            {
                if(!body.Alive||body.BurnRemaining<=0)continue;
                float elapsed=Math.Min(dt,body.BurnRemaining);body.BurnRemaining=Math.Max(0,body.BurnRemaining-elapsed);body.BurnClock+=elapsed;
                if(body.BurnClock>=.5f||body.BurnRemaining<=0)
                {
                    float amount=body.BurnDps*body.BurnClock;body.BurnClock=0;
                    ApplyDamage(body,amount,false);EmitText(body,$"화상 {amount:0.#}",FeedbackKind.Burn);
                }
                if(body.BurnRemaining<=0)body.BurnDps=0;
            }
        }
    }
}
