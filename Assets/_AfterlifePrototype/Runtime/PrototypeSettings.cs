using UnityEngine;

namespace Afterlife.Prototype
{
    // 인스펙터 데이터에서 순수 모델 설정으로 복사한다. 모델은 에셋을 참조하지 않는다.
    [CreateAssetMenu(menuName = "Afterlife/Prototype Settings")]
    public sealed class PrototypeSettings : ScriptableObject
    {
        [Header("초기 밸런스 · 검증 전")]
        public float FirstPreparation = 60;
        public float Maintenance = 45;
        public float CursorRadius = .65f;
        public float HouseRadius = 3.8f;
        public float WoodPerSecond = 2;
        public float StonePerSecond = 1;
        public float CursorDamage = 8;
        public float RepairPerSecond = 12;
        public float WarriorSeconds = 10;
        public int WarriorMoney = 10;
        [Header("낮밤 · 시야 · 자동 기여")]
        public float DaySeconds = 35;
        public float DuskSeconds = 5;
        public float NightSeconds = 35;
        public float ContributionSeconds = 2;
        [Min(.1f)] public float ContributionPower = 1;
        public float CursorVision = 7;
        public float VillageVision = 9;
        public float VisionTrailSeconds = 1.4f;
        public float AutoConstruction = .25f;
        [Header("자원 재생성")]
        public float ResourceSpawnSeconds = 12;
        public int TreeLimit = 260;
        public int RockLimit = 160;
        public int MapRadius = 45;
        [Range(3,15)] public int PrototypeDays = 6;
        [Header("화면 가장자리 거리별 카메라 속도 (픽셀)")]
        public float CameraEdgeBand = 160;
        public float CameraFastBand = 35;
        public float CameraMediumBand = 85;
        public float CameraSlowSpeed = 4;
        public float CameraMediumSpeed = 10;
        public float CameraMaxSpeed = 20;
        [Header("표시")]
        [Range(180, 720)] public int PixelHeight = 360;
        [Header("효과음")]
        [Range(0, 1)] public float SoundVolume = .7f;
        public Balance ToBalance() => new Balance
        {
            FirstPreparation = FirstPreparation, Maintenance = Maintenance,
            CursorRadius = CursorRadius, HouseRadius = HouseRadius,
            WoodRate = WoodPerSecond, StoneRate = StonePerSecond,
            CursorDamage = CursorDamage, RepairRate = RepairPerSecond,
            WarriorSeconds = WarriorSeconds, WarriorMoney = WarriorMoney,
            DaySeconds = DaySeconds, DuskSeconds = DuskSeconds, NightSeconds = NightSeconds,
            PulseSeconds = ContributionSeconds, ContributionPower = ContributionPower, CursorVision = CursorVision, VillageVision = VillageVision,
            VisionTrailSeconds = VisionTrailSeconds, AutoConstruction = AutoConstruction, MapRadius = MapRadius, Days = PrototypeDays,ResourceSpawnSeconds=ResourceSpawnSeconds,TreeLimit=TreeLimit,RockLimit=RockLimit
        };
    }
}
