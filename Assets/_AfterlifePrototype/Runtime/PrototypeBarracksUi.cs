using System;
using System.Linq;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        private readonly Order[] _displayedReservations=new Order[VillageModel.MaxQueuedOrders];
        private int _displayedBarracks;
        private static readonly Kind[] RecruitKinds={Kind.Warrior,Kind.Archer,Kind.Mage,Kind.Knight};

        private void CancelDisplayedReservation(int slot)
        {
            if(_modal!=Modal.Barracks||_displayedBarracks!=_modalBody||slot<0||slot>=_displayedReservations.Length)return;
            _model.CancelQueuedOrder(_modalBody,_displayedReservations[slot]);
        }

        private void UpdateBarracksUi(Body barracks)
        {
            var ui=_sceneUi;var orders=barracks.Orders.ToArray();
            var current=orders.Length>0?orders[0]:null;
            int waiting=Math.Max(0,orders.Length-1);
            _displayedBarracks=barracks.Id;
            string state=current==null?"병종을 선택하면 생산을 시작합니다.":barracks.MovingOut?"병영 이동 중 · 생산 대기":
                current.Progress>=1?"생산 완료 · 배치 공간 확보 대기":
                current.Progress<=0&&!_model.Unlocked(current.Kind)?"선행 건물 필요 · 생산 대기":$"남은 시간 {Math.Max(0,current.Seconds*(1-current.Progress)):0.0}초";
            ui.Label("Barracks.Current",current==null?"생산 대기":VillageModel.Name(current.Kind)+" 생산 중");
            ui.Label("Barracks.Percent",current==null?"0%":$"{Mathf.Clamp01(current.Progress)*100:0}%");
            ui.Label("Barracks.Status",state);
            var progress=ui.Item("Barracks.Progress")?.Image;
            if(progress!=null)progress.fillAmount=current==null?0:Mathf.Clamp01(current.Progress);
            ui.Label("Barracks.Queue",$"생산 예약 {waiting} / {VillageModel.MaxQueuedOrders}");
            ui.Enable("Barracks.Cancel",current!=null&&!_model.Frozen);
            for(int slot=0;slot<_displayedReservations.Length;slot++)
            {
                var order=slot+1<orders.Length?orders[slot+1]:null;_displayedReservations[slot]=order;
                string id="Barracks.Slot."+slot;
                ui.Label(id,$"{slot+1:00}\n"+(order==null?"—":VillageModel.Name(order.Kind)));
                ui.Enable(id,order!=null&&!_model.Frozen);
            }
            bool full=orders.Length>=VillageModel.MaxQueuedOrders+1;
            foreach(var kind in RecruitKinds)
            {
                bool unlocked=_model.Unlocked(kind),funds=_model.Money>=_model.Price(kind);
                string requirement=kind==Kind.Mage?"제단 필요":"연구소 필요";
                string action=!unlocked?requirement:full?"예약 가득 참":!funds?"골드 부족":barracks.MovingOut?"이동 중":_model.Frozen?"생산 불가":"생산 예약";
                ui.Label("Recruit."+kind,action);
                ui.Enable("Recruit."+kind,unlocked&&funds&&!full&&!_model.Frozen&&!barracks.MovingOut&&barracks.Complete);
                string role=kind==Kind.Warrior?"근접 · 기본 전투병":kind==Kind.Archer?"원거리 · 후방 지원":kind==Kind.Mage?"원거리 · 높은 공격력":"근접 · 높은 체력";
                ui.Label("Barracks.Info."+kind,$"{VillageModel.Name(kind)} · {role}\n체력 {VillageModel.TroopHealth(kind):0}   공격 {_model.TroopDamage(kind):0}   사거리 {_model.TroopRange(kind):0.##}\n골드 {_model.Price(kind)} · 생산 {_model.RecruitmentSeconds(kind):0.#}초");
            }
        }
    }
}
