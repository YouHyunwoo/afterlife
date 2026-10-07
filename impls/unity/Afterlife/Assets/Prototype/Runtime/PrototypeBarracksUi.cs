using System;
using System.Linq;
using UnityEngine;

namespace Afterlife.Prototype
{
    public sealed partial class PrototypeView
    {
        // 기존 생산 모달의 프리팹과 바인딩을 재사용한다. 예약 슬롯과 수동 모집은 표시하지 않는다.
        private void UpdateBarracksUi(Body building)
        {
            var ui = _sceneUi;
            var stats = _model.Building(building.Kind);
            float progress = Mathf.Clamp01(_model.ProductionProgress(building.Id));
            float remaining = Mathf.Max(0, _model.ProductionRemaining(building.Id));
            bool troops = stats.Production == ProductionKind.Troop;
            int owned = troops ? _model.OwnedTroopCount(building.Id) : 0;
            bool full = troops && owned >= stats.TroopCapacity;
            string product = troops ? _model.DisplayName(stats.ProducedUnit)
                : stats.Production == ProductionKind.Etheron ? "에테론" : "보리";
            string status = building.MovingOut ? "이전 중 · 생산 대기"
                : full ? "정원 충족 · 병력이 줄면 새 회차 시작"
                : _model.Frozen ? "게임 정지 · 생산 대기"
                : $"남은 시간 {remaining:0.0}초";

            ui.Label("Barracks.Current", $"{_model.DisplayName(building.Kind)} · {product} 자동 생산");
            ui.Label("Barracks.Percent", full ? "정원 충족" : $"{progress * 100:0}%");
            ui.Label("Barracks.Status", status);
            var progressImage = ui.Item("Barracks.Progress")?.Image;
            if (progressImage != null) progressImage.fillAmount = full ? 0 : progress;

            int originX = Mathf.RoundToInt(building.Position.X - (building.Width - 1) * .5f);
            int originZ = Mathf.RoundToInt(building.Position.Z - (building.Depth - 1) * .5f);
            string site = _model.EconomicSiteTooltip(building.Kind, originX, originZ, building.Width, building.Depth);
            int outputCount = stats.ProductionAmount * (string.IsNullOrEmpty(site) ? 1 : 2);
            string output = troops ? $"소속 병력 {owned} / {stats.TroopCapacity} · {stats.ProductionSeconds:0.#}초마다 1명"
                : stats.Production == ProductionKind.Gold
                    ? $"기본 {stats.ProductionSeconds:0.#}초마다 보리 {outputCount}개 · 골드 {outputCount}"
                    : $"{stats.ProductionSeconds:0.#}초마다 {product} {outputCount}개";
            if (!string.IsNullOrEmpty(site)) output += "\n" + site;
            if (building.Kind == Kind.Farm)
            {
                int adjacent = _model.Bodies.Count(other => other.Alive && other.Complete && !other.MovingOut
                    && other.Kind == Kind.Farm && other.Id != building.Id
                    && Mathf.Abs(other.Position.X - building.Position.X) + Mathf.Abs(other.Position.Z - building.Position.Z) < 1.01f);
                output += $"\n인접 농장 {Math.Min(4, adjacent)}개 · 생산 속도 +{Math.Min(4, adjacent) * 25}%";
            }
            ui.Label("Barracks.Queue", output);

            bool canAccelerate = stats.AccelerationGoldCost > 0 && !building.MovingOut && !full
                && !_model.Frozen && !IsGameplayPausedByModal() && Time.timeScale > 0
                && remaining > 0 && _model.Money >= stats.AccelerationGoldCost;
            ui.Show("Barracks.Cancel", stats.AccelerationGoldCost > 0);
            ui.Label("Barracks.Cancel", $"1초 가속 · 골드 {stats.AccelerationGoldCost}");
            ui.Enable("Barracks.Cancel", canAccelerate);
            for (int slot = 0; slot < VillageModel.MaxQueuedOrders; slot++) ui.Show("Barracks.Slot." + slot, false);
            foreach (var kind in new[] { Kind.Warrior, Kind.Archer, Kind.Mage, Kind.Knight })
            {
                ui.Show("Recruit." + kind, false);
                ui.Show("Barracks.Info." + kind, false);
            }
        }
    }
}
