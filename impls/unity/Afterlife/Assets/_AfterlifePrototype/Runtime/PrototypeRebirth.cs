using System;

namespace Afterlife.Prototype
{
    public enum MemoryUpgrade { Work, House, Army }
    public sealed class RebirthReceipt
    {
        public int Cleared, Kills, WaveReward, KillBonus, StockBonus;
        public int Total=>WaveReward+KillBonus+StockBonus;
    }
    public sealed class RunResult { public bool Victory; public int BestCleared, Rebirths, Kills; }

    public sealed partial class VillageModel
    {
        public int Etheron {get;private set;}
        public int MemoryWork {get;private set;}
        public int MemoryHouse {get;private set;}
        public int MemoryArmy {get;private set;}
        public int RebirthCount {get;private set;}
        public int SegmentCleared {get;private set;}
        public int SegmentKills {get;private set;}
        public int RunBestCleared {get;private set;}
        public int RunKills {get;private set;}
        // 판 데이터와 별도의 영구 외형 해금 예제. 디스크 접근은 View가 담당한다.
        public bool BannerUnlocked {get;set;}
        public RebirthReceipt LastRebirth {get;private set;}
        public RunResult LastResult {get;private set;}
        public float MemoryWorkMultiplier=>1+.20f*MemoryWork;
        public float MemoryArmyMultiplier=>1+.20f*MemoryArmy;
        public bool CanRebirth=>!Ended&&!DailyRewardPending&&Has(Kind.Altar)&&SegmentCleared>0&&SegmentCleared<Settings.Days&&(Phase==Phase.Day||Phase==Phase.Settlement);
        private void ClearRunProgress()
        {
            Etheron=MemoryWork=MemoryHouse=MemoryArmy=RebirthCount=RunBestCleared=RunKills=0;LastRebirth=null;
        }
        public RebirthReceipt PreviewRebirth()
        {
            int reward=0;for(int wave=1;wave<=SegmentCleared;wave++)reward+=2*wave*wave;
            int cap=reward/2,killBonus=Math.Min(cap,SegmentKills/10);
            int stockBonus=Math.Min(cap-killBonus,(int)Math.Floor(Money/100));
            return new RebirthReceipt{Cleared=SegmentCleared,Kills=SegmentKills,WaveReward=reward,KillBonus=killBonus,StockBonus=stockBonus};
        }
        public bool BeginRebirth()
        {
            if(!CanRebirth)return false;
            LastRebirth=PreviewRebirth();Etheron+=LastRebirth.Total;RebirthCount++;
            ResetAttempt();Phase=Phase.Rebirth;Working=PointerVisible=false;
            Sound(SoundCue.Rebirth);
            Say($"환생 정산 완료 · 에테론 +{LastRebirth.Total}. 강화 후 첫날부터 재건하세요.");return true;
        }
        public int MemoryLevel(MemoryUpgrade upgrade)=>upgrade==MemoryUpgrade.Work?MemoryWork:upgrade==MemoryUpgrade.House?MemoryHouse:MemoryArmy;
        public int MemoryPrice(MemoryUpgrade upgrade)=>8*(1<<MemoryLevel(upgrade));
        public void BuyMemory(MemoryUpgrade upgrade)
        {
            if(Phase!=Phase.Rebirth||MemoryLevel(upgrade)>=3||Etheron<MemoryPrice(upgrade))return;
            Etheron-=MemoryPrice(upgrade);
            if(upgrade==MemoryUpgrade.Work)MemoryWork++;else if(upgrade==MemoryUpgrade.House)MemoryHouse++;else MemoryArmy++;
        }
        public bool FinishRebirth()
        {
            if(Phase!=Phase.Rebirth)return false;
            ResetAttempt();Sound(SoundCue.Rebirth);Say($"환생 {RebirthCount}회 · 강화와 미사용 에테론 {Etheron} 유지. 첫날부터 시작합니다.");return true;
        }
        private void ApplyDamage(Body target,float damage,bool stagger=true)
        {
            if(target==null||!target.Alive||damage<=0)return;
            if(target.SpiritShield>0){float absorbed=Math.Min(target.SpiritShield,damage);target.SpiritShield-=absorbed;damage-=absorbed;}
            target.Health-=damage;
            if(target.Building)Sound(target.Alive?SoundCue.BuildingHit:SoundCue.Collapse);
            else if(!target.Alive&&target.Hostile)Sound(SoundCue.Kill,target.Position);
            if(target.Unit&&stagger){target.StunRemaining=.2f;target.StunUntil=_combatTime+.2f;target.PendingTarget=0;target.AttackWindup=target.AttackRecovery=0;}
            if(target.Kind==Kind.Animal){target.FleeUntil=_combatTime+5;target.FleeFrom=_activeDivineFieldCenter??(Working?WorkCenter:target.Position);}
            if(!target.Alive){if(target.Hostile){SegmentKills++;RunKills++;}CollectKillLoot(target);}
            Damaged?.Invoke(target);
        }
    }
}
